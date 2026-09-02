using Ibtikar.Data;
using Ibtikar.DTOs.Committee;
using Ibtikar.Models;
using Ibtikar.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public sealed class CommitteeDashboardRepository : ICommitteeDashboardRepository
    {
        private readonly IbtikarDbContext _db;

        public CommitteeDashboardRepository(IbtikarDbContext db) => _db = db;

        public async Task<CommitteeDashboardDto> GetSnapshotCountsAsync(CancellationToken ct)
        {
            var underStudy = await _db.InnovationIdeas.AsNoTracking()
                .CountAsync(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.ReferredCommittee, ct);

            var underVoting = await _db.InnovationIdeas.AsNoTracking()
                .CountAsync(i => i.CurrentStatus != null
                    && (i.CurrentStatus.Code == IdeaStatusCodes.ReferredCommittee
                        || i.CurrentStatus.Code == IdeaStatusCodes.UnderAssessment), ct);

            var accepted = await _db.InnovationIdeas.AsNoTracking()
                .CountAsync(i => i.CurrentStatus != null
                    && (i.CurrentStatus.Code == IdeaStatusCodes.Approved
                        || i.CurrentStatus.Code == IdeaStatusCodes.InExecution), ct);

            var rejected = await _db.InnovationIdeas.AsNoTracking()
                .CountAsync(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.Rejected, ct);

            return new CommitteeDashboardDto(underStudy, underVoting, accepted, rejected);
        }

        public async Task<IReadOnlyList<CommitteeReferralRowDto>> GetReferralsAsync(Guid userId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            var rows = await _db.InnovationIdeas.AsNoTracking()
                .Where(i => i.CurrentStatus != null
                    && (i.CurrentStatus.Code == IdeaStatusCodes.ReferredCommittee
                        || i.CurrentStatus.Code == IdeaStatusCodes.Approved
                        || i.CurrentStatus.Code == IdeaStatusCodes.Rejected
                        || i.CurrentStatus.Code == IdeaStatusCodes.ReturnedForDevelopment
                        || i.CurrentStatus.Code == IdeaStatusCodes.InExecution))
                .OrderByDescending(i => i.CreatedAt)
                .Take(100)
                .Select(i => new CommitteeReferralRowDto(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d",
                    i.ApplicantUser != null ? i.ApplicantUser.FullName : null,
                    i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : null,
                    i.AuditAssignedAt,
                    i.AuditAssignedAt.HasValue ? (now - i.AuditAssignedAt.Value).TotalDays : 0.0,
                    i.AuditAssignedAt.HasValue && (now - i.AuditAssignedAt.Value) > TimeSpan.FromDays(4)))
                .ToListAsync(ct);

            if (rows.Count == 0) return rows;

            var ideaIds = rows.Select(r => r.IdeaId).ToHashSet();
            var criteriaCount = await CountActiveCriteriaAsync(ct);

            var specializedHeaders = await _db.AssessmentHeaders.AsNoTracking()
                .Include(h => h.Details)
                .Where(h => ideaIds.Contains(h.InnovationIdeaId)
                    && h.Source == AssessmentHeader.SourceSpecialized
                    && !h.IsDraft)
                .ToListAsync(ct);

            var committeeHeaders = await _db.AssessmentHeaders.AsNoTracking()
                .Include(h => h.Details)
                .Where(h => ideaIds.Contains(h.InnovationIdeaId)
                    && h.Source == AssessmentHeader.SourceCommittee
                    && !h.IsDraft)
                .ToListAsync(ct);

            var myCommitteeHeaders = committeeHeaders
                .Where(h => h.AssessorUserId == userId)
                .ToList();

            var deptByIdea = specializedHeaders
                .GroupBy(h => h.InnovationIdeaId)
                .Select(g => g.OrderByDescending(h => h.SubmittedAt ?? h.CreatedAt).First())
                .ToDictionary(h => h.InnovationIdeaId, h => CommitteeReferralPercent(h.Details.Sum(d => d.Score), criteriaCount));

            var committeeByIdea = committeeHeaders
                .GroupBy(h => h.InnovationIdeaId)
                .Select(g => g.OrderByDescending(h => h.SubmittedAt ?? h.CreatedAt).First())
                .ToDictionary(h => h.InnovationIdeaId, h => CommitteeReferralPercent(h.Details.Sum(d => d.Score), criteriaCount));

            var myByIdea = myCommitteeHeaders.ToDictionary(
                h => h.InnovationIdeaId,
                h => CommitteeReferralPercent(h.Details.Sum(d => d.Score), criteriaCount));

            var mySubmittedIds = myCommitteeHeaders.Select(h => h.InnovationIdeaId).ToHashSet();

            return rows.Select(r => r with
            {
                DepartmentPercent = deptByIdea.TryGetValue(r.IdeaId, out var d) ? d : null,
                CommitteePercent = committeeByIdea.TryGetValue(r.IdeaId, out var c) ? c : null,
                MyCommitteePercent = myByIdea.TryGetValue(r.IdeaId, out var m) ? m : null,
                HasAddedCommitteeAssessment = mySubmittedIds.Contains(r.IdeaId)
            }).ToList();
        }

        private static int? CommitteeReferralPercent(int scoreSum, int criteriaCount)
            => criteriaCount <= 0
                ? null
                : (int)Math.Round(scoreSum / (criteriaCount * (double)5) * 100, MidpointRounding.AwayFromZero);

        public async Task<CommitteeAssessIdeaDto?> GetAssessIdeaAsync(Guid ideaId, CancellationToken ct)
            => await _db.InnovationIdeas.AsNoTracking()
                .Where(i => i.Id == ideaId)
                .Select(i => new CommitteeAssessIdeaDto(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d"))
                .FirstOrDefaultAsync(ct);

        public async Task<IReadOnlyList<CommitteeCriterionDto>> GetActiveCriteriaAsync(CancellationToken ct)
            => await _db.AssessmentCriteria.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CommitteeCriterionDto(c.Id, c.Code, c.Name, c.Description, c.DisplayOrder))
                .ToListAsync(ct);

        public Task<int> CountActiveCriteriaAsync(CancellationToken ct)
            => _db.AssessmentCriteria.CountAsync(c => c.IsActive, ct);

        public async Task<AssessmentHeader?> GetLatestCommitteeHeaderAsync(Guid ideaId, Guid userId, CancellationToken ct)
            => await _db.AssessmentHeaders.AsNoTracking()
                .Include(h => h.Details)
                .Where(h => h.InnovationIdeaId == ideaId
                    && h.AssessorUserId == userId
                    && h.Source == AssessmentHeader.SourceCommittee)
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync(ct);

        public async Task<AssessmentHeader?> GetCommitteeHeaderForSaveAsync(Guid ideaId, Guid userId, Guid? headerId, CancellationToken ct)
            => await _db.AssessmentHeaders
                .Include(h => h.Details)
                .FirstOrDefaultAsync(h => h.Id == headerId, ct)
                ?? await _db.AssessmentHeaders
                    .Include(h => h.Details)
                    .Where(h => h.InnovationIdeaId == ideaId
                        && h.AssessorUserId == userId
                        && h.Source == AssessmentHeader.SourceCommittee)
                    .OrderByDescending(h => h.CreatedAt)
                    .FirstOrDefaultAsync(ct);

        public async Task<AssessmentHeader?> GetLatestSubmittedHeaderAsync(Guid ideaId, string source, CancellationToken ct)
            => await _db.AssessmentHeaders.AsNoTracking()
                .Include(h => h.Details)
                .Where(h => h.InnovationIdeaId == ideaId
                    && h.Source == source
                    && !h.IsDraft)
                .OrderByDescending(h => h.SubmittedAt ?? h.CreatedAt)
                .FirstOrDefaultAsync(ct);

        public void AddAssessmentHeader(AssessmentHeader header)
            => _db.AssessmentHeaders.Add(header);

        public void RemoveAssessmentDetails(IEnumerable<AssessmentDetail> details)
            => _db.AssessmentDetails.RemoveRange(details);

        public async Task<bool> IdeaExistsAsync(Guid ideaId, CancellationToken ct)
            => await _db.InnovationIdeas.AsNoTracking().AnyAsync(i => i.Id == ideaId, ct);

        public async Task<Guid?> GetIdeaCurrentStatusIdAsync(Guid ideaId, CancellationToken ct)
            => await _db.InnovationIdeas.AsNoTracking()
                .Where(i => i.Id == ideaId)
                .Select(i => (Guid?)i.CurrentStatusId)
                .FirstOrDefaultAsync(ct);

        public async Task<string?> GetStatusCodeByIdAsync(Guid statusId, CancellationToken ct)
            => await _db.IdeaStatuses.AsNoTracking()
                .Where(s => s.Id == statusId)
                .Select(s => s.Code)
                .FirstOrDefaultAsync(ct);

        public async Task<Guid?> GetStatusIdByCodeAsync(string code, CancellationToken ct)
            => await _db.IdeaStatuses
                .AsNoTracking()
                .Where(s => s.Code == code)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);

        public async Task<InnovationIdea?> GetIdeaWithStatusAsync(Guid ideaId, CancellationToken ct)
            => await _db.InnovationIdeas
                .Include(i => i.CurrentStatus)
                .FirstOrDefaultAsync(i => i.Id == ideaId, ct);

        public async Task<IReadOnlyList<CommitteeVoteIdeaDto>> GetVoteIdeasAsync(CancellationToken ct)
            => await _db.InnovationIdeas.AsNoTracking()
                .Where(i => i.CurrentStatus != null
                    && (i.CurrentStatus.Code == IdeaStatusCodes.ReferredCommittee
                        || i.CurrentStatus.Code == IdeaStatusCodes.UnderAssessment))
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new CommitteeVoteIdeaDto(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d"))
                .ToListAsync(ct);

        public async Task<IReadOnlyDictionary<Guid, string>> GetVotesByUserAsync(Guid userId, IReadOnlyCollection<Guid> ideaIds, CancellationToken ct)
            => await _db.CommitteeVotes.AsNoTracking()
                .Where(v => v.MemberUserId == userId && ideaIds.Contains(v.InnovationIdeaId))
                .ToDictionaryAsync(v => v.InnovationIdeaId, v => v.Decision, ct);

        public async Task<bool> HasVotedAsync(Guid ideaId, Guid userId, CancellationToken ct)
            => await _db.CommitteeVotes.AsNoTracking()
                .AnyAsync(v => v.InnovationIdeaId == ideaId && v.MemberUserId == userId, ct);

        public async Task AddVoteAsync(CommitteeVote vote, CancellationToken ct)
        {
            await _db.CommitteeVotes.AddAsync(vote, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<CommitteeDecisionIdeaDto?> GetDecisionIdeaAsync(Guid ideaId, CancellationToken ct)
            => await _db.InnovationIdeas.AsNoTracking()
                .Where(i => i.Id == ideaId)
                .Select(i => new CommitteeDecisionIdeaDto(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty))
                .FirstOrDefaultAsync(ct);

        public async Task AddStatusHistoryAndSaveAsync(IdeaStatusHistory history, CancellationToken ct)
        {
            await _db.IdeaStatusHistories.AddAsync(history, ct);
            await _db.SaveChangesAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}