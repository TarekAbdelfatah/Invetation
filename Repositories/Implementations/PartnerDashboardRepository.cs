using Ibtikar.Data;
using Ibtikar.DTOs.PartnerDashboard;
using Ibtikar.Models;
using Ibtikar.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public interface IPartnerDashboardRepository
    {
        Task<PartnerDashboardDto> GetSnapshotAsync(Guid departmentId, CancellationToken ct);
        Task<PartnerInboxDto> GetInboxAsync(Guid departmentId, CancellationToken ct);
        Task<PartnerDetailsDto?> GetDetailsAsync(Guid assignmentId, Guid departmentId, CancellationToken ct);
        Task<AssessmentHeader?> GetExistingPartnerHeaderAsync(Guid ideaId, Guid departmentId, CancellationToken ct);
        Task<AssessmentHeader?> GetSpecializedAssessmentAsync(Guid ideaId, CancellationToken ct);
        Task<PartnerAssignment?> GetAssignmentForPartnerAsync(Guid assignmentId, Guid departmentId, CancellationToken ct);
        Task AddOrUpdatePartnerHeaderAsync(AssessmentHeader header, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }

    public sealed class PartnerDashboardRepository : IPartnerDashboardRepository
    {
        private const int LateThresholdDays = 4;
        private static readonly TimeSpan LateThreshold = TimeSpan.FromDays(LateThresholdDays);

        private readonly IbtikarDbContext _db;
        private readonly IPartnerAssignmentQuery _query;

        public PartnerDashboardRepository(IbtikarDbContext db, IPartnerAssignmentQuery query)
        {
            _db = db;
            _query = query;
        }

        public async Task<PartnerDashboardDto> GetSnapshotAsync(Guid departmentId, CancellationToken ct)
        {
            var assignments = _query.ForDepartment(_db.PartnerAssignments.AsNoTracking(), departmentId);

            var pending = await assignments.CountAsync(p => p.Status == PartnerAssignment.StatusPending, ct);

            var late = await assignments.CountAsync(p =>
                p.Status == PartnerAssignment.StatusPending
                && (DateTime.UtcNow - p.SentAt) > LateThreshold, ct);

            var cycleStart = DateTime.UtcNow.AddDays(-30);
            var submitted = await assignments.CountAsync(p =>
                p.Status == PartnerAssignment.StatusSubmitted
                && p.RespondedAt.HasValue
                && p.RespondedAt.Value >= cycleStart, ct);

            return new PartnerDashboardDto(pending, late, submitted);
        }

        public async Task<PartnerInboxDto> GetInboxAsync(Guid departmentId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var rows = await _query.ForDepartment(_db.PartnerAssignments.AsNoTracking(), departmentId)
                .OrderByDescending(p => p.SentAt)
                .Select(p => new PartnerAssignmentRowDto(
                    p.Id,
                    p.InnovationIdeaId,
                    p.InnovationIdea!.ReferenceNumber,
                    p.InnovationIdea.Title,
                    p.InnovationIdea.ApplicantUser != null ? p.InnovationIdea.ApplicantUser.FullName : "—",
                    p.InnovationIdea.AssignedDepartment != null ? p.InnovationIdea.AssignedDepartment.Name : "—",
                    p.SentAt,
                    p.RespondedAt,
                    p.Status,
                    p.Status == PartnerAssignment.StatusPending && (now - p.SentAt) > LateThreshold,
                    p.Status == PartnerAssignment.StatusPending,
                    p.Status == PartnerAssignment.StatusReturned,
                    (now - p.SentAt).TotalDays))
                .ToListAsync(ct);

            return new PartnerInboxDto(rows, rows.Count);
        }

        public async Task<PartnerDetailsDto?> GetDetailsAsync(Guid assignmentId, Guid departmentId, CancellationToken ct)
        {
            var assignment = await _query.ForDepartment(_db.PartnerAssignments.AsNoTracking(), departmentId)
                .Include(p => p.InnovationIdea).ThenInclude(i => i.InnovationDomain)
                .Include(p => p.InnovationIdea).ThenInclude(i => i.ApplicantUser)
                .Include(p => p.InnovationIdea).ThenInclude(i => i.ApplicantDepartment)
                .Include(p => p.InnovationIdea).ThenInclude(i => i.AssignedDepartment)
                .FirstOrDefaultAsync(p => p.Id == assignmentId, ct);

            if (assignment?.InnovationIdea is null) return null;
            var idea = assignment.InnovationIdea;

            var criteria = await _db.AssessmentCriteria
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new PartnerCriterionDto(c.Id, c.Code, c.Name, c.DisplayOrder))
                .ToListAsync(ct);

            var existing = await _db.AssessmentHeaders
                .AsNoTracking()
                .Include(h => h.Details)
                .Where(h => h.InnovationIdeaId == idea.Id
                    && h.AssessorDepartmentId == departmentId
                    && h.Source == AssessmentHeader.SourcePartner)
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var lineMap = existing?.Details.ToDictionary(d => d.CriterionId, d => (d.Score, d.Comment))
                ?? new Dictionary<Guid, (int, string?)>();

            var lines = criteria.Select(c => lineMap.TryGetValue(c.Id, out var v)
                ? new PartnerScoreLineDto(c.Id, c.Code, c.Name, v.Item1, v.Item2)
                : new PartnerScoreLineDto(c.Id, c.Code, c.Name, null, null))
                .ToList();

            var canScore = assignment.Status == PartnerAssignment.StatusPending;
            var isNotCompetentReturn = assignment.Status == PartnerAssignment.StatusReturned
                && assignment.Note?.StartsWith("NotCompetent:", StringComparison.Ordinal) == true;
            var notCompetentReason = isNotCompetentReturn && assignment.Note is { Length: > 14 }
                ? assignment.Note[14..].Trim()
                : null;

            var specialized = await GetSpecializedAssessmentAsync(idea.Id, ct);
            var specializedDto = specialized is null
                ? new PartnerSpecializedAssessmentDto(
                    HasAssessment: false,
                    AssessorDepartmentName: idea.AssignedDepartment?.Name ?? "—",
                    TotalScore: null,
                    Comment: null,
                    SubmittedAt: null,
                    Scores: Array.Empty<PartnerSpecializedScoreDto>())
                : new PartnerSpecializedAssessmentDto(
                    HasAssessment: true,
                    AssessorDepartmentName: specialized.AssessorDepartment?.Name ?? "—",
                    TotalScore: specialized.TotalScore,
                    Comment: specialized.Comment,
                    SubmittedAt: specialized.SubmittedAt,
                    Scores: specialized.Details
                        .OrderBy(d => d.Criterion?.DisplayOrder ?? 0)
                        .Select(d => new PartnerSpecializedScoreDto(
                            d.CriterionId,
                            d.Criterion?.Code ?? string.Empty,
                            d.Criterion?.Name ?? string.Empty,
                            d.Score,
                            d.Comment))
                        .ToList());

            return new PartnerDetailsDto(
                assignment.Id, idea.Id, idea.ReferenceNumber, idea.Title,
                idea.Description, idea.ProblemStatement, idea.ProposedSolution, idea.ExpectedBenefits,
                idea.InnovationDomain?.Name,
                idea.ApplicantUser?.FullName ?? "—",
                idea.ApplicantDepartment?.Name ?? "خارجي",
                idea.AssignedDepartment?.Name ?? "—",
                assignment.Status, assignment.SentAt, assignment.RespondedAt,
                canScore, existing is not null && !existing.IsDraft,
                criteria, lines, existing?.TotalScore, existing?.Comment,
                isNotCompetentReturn, notCompetentReason,
                false,
                specializedDto);
        }

        public async Task<AssessmentHeader?> GetExistingPartnerHeaderAsync(Guid ideaId, Guid departmentId, CancellationToken ct)
        {
            return await _db.AssessmentHeaders
                .Include(h => h.Details)
                .Where(h => h.InnovationIdeaId == ideaId
                    && h.AssessorDepartmentId == departmentId
                    && h.Source == AssessmentHeader.SourcePartner)
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<AssessmentHeader?> GetSpecializedAssessmentAsync(Guid ideaId, CancellationToken ct)
        {
            return await _db.AssessmentHeaders
                .AsNoTracking()
                .Include(h => h.Details)
                    .ThenInclude(d => d.Criterion)
                .Include(h => h.AssessorDepartment)
                .Where(h => h.InnovationIdeaId == ideaId
                    && h.Source == AssessmentHeader.SourceSpecialized
                    && !h.IsDraft)
                .OrderByDescending(h => h.SubmittedAt ?? h.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<PartnerAssignment?> GetAssignmentForPartnerAsync(Guid assignmentId, Guid departmentId, CancellationToken ct)
        {
            return await _query.ForDepartment(_db.PartnerAssignments, departmentId)
                .FirstOrDefaultAsync(p => p.Id == assignmentId, ct);
        }

        public async Task AddOrUpdatePartnerHeaderAsync(AssessmentHeader header, CancellationToken ct)
        {
            var existing = await _db.AssessmentHeaders
                .Include(h => h.Details)
                .FirstOrDefaultAsync(h => h.Id == header.Id, ct);

            if (existing is null)
            {
                await _db.AssessmentHeaders.AddAsync(header, ct);
            }
            else
            {
                existing.IsDraft = header.IsDraft;
                existing.IsLocked = header.IsLocked;
                existing.TotalScore = header.TotalScore;
                existing.Comment = header.Comment;
                existing.SubmittedAt = header.SubmittedAt;
                existing.LockedAt = header.LockedAt;

                _db.AssessmentDetails.RemoveRange(existing.Details);
                foreach (var d in header.Details)
                {
                    d.AssessmentHeaderId = existing.Id;
                    await _db.AssessmentDetails.AddAsync(d, ct);
                }
            }
        }

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}