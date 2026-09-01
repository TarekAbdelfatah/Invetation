using Ibtikar.Data;
using Ibtikar.DTOs.SpecializedDashboard;
using Ibtikar.Models;
using Ibtikar.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public interface ISpecializedDashboardRepository
    {
        Task<SpecializedDashboardDto> GetSnapshotAsync(Guid departmentId, CancellationToken ct);
        Task<SpecializedReferralsDto> GetReferralsAsync(Guid departmentId, string statusFilter, int page, int pageSize, CancellationToken ct);
        Task<SpecializedDetailsDto?> GetDetailsAsync(Guid ideaId, Guid departmentId, CancellationToken ct);
        Task<SpecializedAssessVmDto?> GetAssessVmAsync(Guid ideaId, Guid departmentId, CancellationToken ct);
        Task<IReadOnlyList<SpecializedPartnerOptionDto>> GetAvailablePartnersAsync(Guid excludeDepartmentId, IReadOnlyCollection<Guid> alreadyAssignedIds, CancellationToken ct);
        Task<IReadOnlyList<SpecializedPartnerOptionDto>> GetAlreadyAssignedPartnersAsync(Guid ideaId, CancellationToken ct);
        Task<SpecializedPartnerOpinionDto?> GetPartnerOpinionAsync(Guid ideaId, Guid departmentId, CancellationToken ct);
        Task<SpecializedSendToCommitteeDto?> GetSendToCommitteeSummaryAsync(Guid ideaId, Guid departmentId, CancellationToken ct);
        Task<AssessmentHeader?> GetDraftHeaderAsync(Guid ideaId, Guid departmentId, CancellationToken ct);
        Task<IReadOnlyList<AssessmentHeader>> GetSpecializedFinalHeadersAsync(Guid ideaId, CancellationToken ct);
        Task<InnovationIdea?> GetIdeaForDepartmentAsync(Guid ideaId, Guid departmentId, CancellationToken ct);
        Task<bool> HasLockedAssessmentAsync(Guid ideaId, Guid departmentId, CancellationToken ct);
        Task<bool> HasPartnerAssignmentsAsync(Guid ideaId, CancellationToken ct);
        Task<IReadOnlyList<PartnerAssignment>> GetPartnerAssignmentsForIdeaAsync(Guid ideaId, CancellationToken ct);
        Task AddPartnerAssignmentsAsync(IEnumerable<PartnerAssignment> rows, CancellationToken ct);
        Task AddOrUpdateAssessmentHeaderAsync(AssessmentHeader header, CancellationToken ct);
        Task AddStatusHistoryAsync(IdeaStatusHistory history, CancellationToken ct);
        Task AddAuditActionAsync(AuditActionItem action, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }

    public sealed class SpecializedDashboardRepository : ISpecializedDashboardRepository
    {
        private const int LateThresholdDays = 4;
        private static readonly TimeSpan LateThreshold = TimeSpan.FromDays(LateThresholdDays);

        private readonly IbtikarDbContext _db;

        public SpecializedDashboardRepository(IbtikarDbContext db) => _db = db;

        public async Task<SpecializedDashboardDto> GetSnapshotAsync(Guid departmentId, CancellationToken ct)
        {
            var routed = _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.AssignedDepartmentId == departmentId);

            var underStudy = await routed.CountAsync(i =>
                i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.UnderStudy, ct);

            var sentToExecution = await routed.CountAsync(i =>
                i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.InExecution, ct);

            var rejectedAfterRouting = await routed.CountAsync(i =>
                i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.Rejected, ct);

            var sentToPartner = await _db.PartnerAssignments
                .AsNoTracking()
                .Where(p => p.InnovationIdea!.AssignedDepartmentId == departmentId
                    && (p.Status == PartnerAssignment.StatusPending || p.Status == PartnerAssignment.StatusLate))
                .Select(p => p.InnovationIdeaId)
                .Distinct()
                .CountAsync(ct);

            return new SpecializedDashboardDto(underStudy, sentToPartner, sentToExecution, rejectedAfterRouting);
        }

        public async Task<SpecializedReferralsDto> GetReferralsAsync(Guid departmentId, string statusFilter, int page, int pageSize, CancellationToken ct)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var now = DateTime.UtcNow;
            var query = _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.AssignedDepartmentId == departmentId);

            query = statusFilter switch
            {
                "under_study" => query.Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.UnderStudy),
                "sent_to_partner" => query.Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.UnderStudy
                    && _db.PartnerAssignments.Any(p => p.InnovationIdeaId == i.Id
                        && (p.Status == PartnerAssignment.StatusPending || p.Status == PartnerAssignment.StatusLate))),
                "in_execution" => query.Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.InExecution),
                "rejected" => query.Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.Rejected),
                _ => query
            };

            var totalCount = await query.CountAsync(ct);

            var rows = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new SpecializedReferralRowDto(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d",
                    i.AuditAssignedAt,
                    i.AuditAssignedAt.HasValue ? (now - i.AuditAssignedAt.Value).TotalDays : 0.0,
                    i.ApplicantUser != null ? i.ApplicantUser.FullName : null,
                    i.AuditAssignedAt.HasValue && (now - i.AuditAssignedAt.Value) > TimeSpan.FromHours(48)))
                .ToListAsync(ct);

            return new SpecializedReferralsDto(rows, statusFilter, page, pageSize, totalCount);
        }

        public async Task<SpecializedDetailsDto?> GetDetailsAsync(Guid ideaId, Guid departmentId, CancellationToken ct)
        {
            var header = await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.Id == ideaId && i.AssignedDepartmentId == departmentId)
                .Select(i => new SpecializedDetailsHeader(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.Description,
                    i.ProblemStatement,
                    i.ProposedSolution,
                    i.ExpectedBenefits,
                    i.InnovationDomain != null ? i.InnovationDomain.Name : null,
                    i.ExpectedImpact != null ? i.ExpectedImpact.Name : null,
                    i.TargetAudience != null ? i.TargetAudience.Name : null,
                    i.ApplicantUser != null ? i.ApplicantUser.FullName : null,
                    i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : null,
                    i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d",
                    i.SubmittedAt,
                    i.AuditAssignedAt))
                .FirstOrDefaultAsync(ct);

            if (header is null) return null;

            var attachments = await _db.IdeaAttachments
                .AsNoTracking()
                .Where(a => a.InnovationIdeaId == ideaId)
                .OrderBy(a => a.UploadedAt)
                .Select(a => new SpecializedAttachmentDto(a.Id, a.FileName, a.SizeBytes, a.UploadedAt))
                .ToListAsync(ct);

            var history = await _db.IdeaStatusHistories
                .AsNoTracking()
                .Where(h => h.InnovationIdeaId == ideaId)
                .OrderByDescending(h => h.ChangedAt)
                .Take(15)
                .Select(h => new SpecializedHistoryRowDto(
                    h.ChangedAt,
                    h.FromStatus != null ? h.FromStatus.Name : "—",
                    h.ToStatus != null ? h.ToStatus.Name : "—",
                    h.ChangedBy != null ? h.ChangedBy.FullName : "—",
                    h.Note))
                .ToListAsync(ct);

            var hasLockedAssessment = await _db.AssessmentHeaders
                .AsNoTracking()
                .AnyAsync(h => h.InnovationIdeaId == ideaId
                    && h.AssessorDepartmentId == departmentId
                    && h.Source == AssessmentHeader.SourceSpecialized
                    && h.IsLocked, ct);

            var hasPartnerRequest = await _db.PartnerAssignments
                .AsNoTracking()
                .AnyAsync(p => p.InnovationIdeaId == ideaId, ct);

            var canReturnNotCompetent = header.StatusCode == IdeaStatusCodes.UnderStudy
                && !hasLockedAssessment
                && !hasPartnerRequest;

            return new SpecializedDetailsDto(
                header.Id, header.Reference, header.Title, header.Description,
                header.ProblemStatement, header.ProposedSolution, header.ExpectedBenefits,
                header.DomainName, header.ExpectedImpactName, header.TargetAudienceName,
                header.ApplicantName, header.ApplicantDepartmentName,
                header.StatusName, header.StatusColor, header.StatusCode,
                header.SubmittedAt, header.AssignedAt, canReturnNotCompetent, attachments, history);
        }

        public async Task<SpecializedAssessVmDto?> GetAssessVmAsync(Guid ideaId, Guid departmentId, CancellationToken ct)
        {
            var header = await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.Id == ideaId && i.AssignedDepartmentId == departmentId)
                .Select(i => new
                {
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    StatusName = i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    StatusColor = i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d"
                })
                .FirstOrDefaultAsync(ct);

            if (header is null) return null;

            var criteria = await _db.AssessmentCriteria
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new SpecializedCriterionDto(c.Id, c.Code, c.Name, c.Description, c.DisplayOrder))
                .ToListAsync(ct);

            var draft = await _db.AssessmentHeaders
                .AsNoTracking()
                .Include(h => h.Details)
                .Where(h => h.InnovationIdeaId == ideaId
                    && h.AssessorDepartmentId == departmentId
                    && h.Source == AssessmentHeader.SourceSpecialized)
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var draftIsLatest = draft is { IsDraft: true, IsLocked: false };
            var lineMap = draftIsLatest && draft is not null
                ? draft.Details.ToDictionary(d => d.CriterionId, d => (d.Score, d.Comment))
                : new Dictionary<Guid, (int, string?)>();

            var lines = criteria.Select(c => lineMap.TryGetValue(c.Id, out var v)
                ? new SpecializedAssessmentLineDto(c.Id, c.Code, c.Name, v.Item1, v.Item2)
                : new SpecializedAssessmentLineDto(c.Id, c.Code, c.Name, null, null))
                .ToList();

            return new SpecializedAssessVmDto(
                header.Id, header.ReferenceNumber, header.Title,
                header.StatusName, header.StatusColor,
                draftIsLatest, draft?.IsLocked ?? false,
                draft?.Id, draft?.CreatedAt,
                criteria, lines,
                draft?.TotalScore, draft?.Comment);
        }

        public async Task<IReadOnlyList<SpecializedPartnerOptionDto>> GetAvailablePartnersAsync(Guid excludeDepartmentId, IReadOnlyCollection<Guid> alreadyAssignedIds, CancellationToken ct)
        {
            var query = _db.Departments
                .AsNoTracking()
                .Where(d => d.IsActive && d.Id != excludeDepartmentId);

            if (alreadyAssignedIds.Count > 0)
            {
                query = query.Where(d => !alreadyAssignedIds.Contains(d.Id));
            }

            return await query
                .OrderBy(d => d.Name)
                .Select(d => new SpecializedPartnerOptionDto(d.Id, d.Name, d.Code))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<SpecializedPartnerOptionDto>> GetAlreadyAssignedPartnersAsync(Guid ideaId, CancellationToken ct)
        {
            return await _db.PartnerAssignments
                .AsNoTracking()
                .Where(p => p.InnovationIdeaId == ideaId)
                .Select(p => new SpecializedPartnerOptionDto(p.PartnerDepartmentId, p.PartnerDepartment!.Name, p.PartnerDepartment.Code))
                .ToListAsync(ct);
        }

        public async Task<SpecializedPartnerOpinionDto?> GetPartnerOpinionAsync(Guid ideaId, Guid departmentId, CancellationToken ct)
        {
            var header = await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.Id == ideaId && i.AssignedDepartmentId == departmentId)
                .Select(i => new { i.Id, i.ReferenceNumber, i.Title })
                .FirstOrDefaultAsync(ct);

            if (header is null) return null;

            var now = DateTime.UtcNow;
            var rows = await _db.PartnerAssignments
                .AsNoTracking()
                .Where(p => p.InnovationIdeaId == ideaId)
                .OrderBy(p => p.SentAt)
                .Select(p => new PartnerOpinionRowRaw(
                    p.Id,
                    p.InnovationIdeaId,
                    p.PartnerDepartmentId,
                    p.PartnerDepartment!.Name,
                    p.Status,
                    p.SentAt,
                    p.RespondedAt,
                    p.Note))
                .ToListAsync(ct);

            // Fetch the latest partner assessment (per partner department) — the response comment + scores.
            var partnerDeptIds = rows.Select(r => r.PartnerDepartmentId).Distinct().ToList();
            var latestAssessments = await _db.AssessmentHeaders
                .AsNoTracking()
                .Include(h => h.Details)
                .Where(h => h.InnovationIdeaId == ideaId
                    && h.Source == AssessmentHeader.SourcePartner
                    && partnerDeptIds.Contains(h.AssessorDepartmentId)
                    && !h.IsDraft)
                .GroupBy(h => h.AssessorDepartmentId)
                .Select(g => g.OrderByDescending(h => h.SubmittedAt ?? h.CreatedAt).First())
                .ToListAsync(ct);

            var assessmentByDept = latestAssessments
                .GroupBy(a => a.AssessorDepartmentId)
                .ToDictionary(g => g.Key, g => g.First());

            var scoreLinesByHeader = await _db.AssessmentDetails
                .AsNoTracking()
                .Where(d => latestAssessments.Select(a => a.Id).Contains(d.AssessmentHeaderId))
                .Join(_db.AssessmentCriteria.AsNoTracking(),
                    d => d.CriterionId,
                    c => c.Id,
                    (d, c) => new
                    {
                        d.AssessmentHeaderId,
                        CriterionId = c.Id,
                        CriterionCode = c.Code,
                        CriterionName = c.Name,
                        d.Score,
                        d.Comment
                    })
                .ToListAsync(ct);

            var scoresByHeader = scoreLinesByHeader
                .GroupBy(s => s.AssessmentHeaderId)
                .ToDictionary(g => g.Key, g => g.Select(s => new SpecializedPartnerScoreLineDto(
                    s.CriterionId, s.CriterionCode, s.CriterionName, s.Score, s.Comment)).ToList());

            var items = rows.Select(p =>
            {
                var daysOpen = (now - p.SentAt).TotalDays;
                var isLate = p.Status == PartnerAssignment.StatusPending && (now - p.SentAt) > LateThreshold;
                var effectiveStatus = isLate && p.Status == PartnerAssignment.StatusPending
                    ? PartnerAssignment.StatusLate
                    : p.Status;
                var badge = effectiveStatus switch
                {
                    PartnerAssignment.StatusSubmitted => "bg-success",
                    PartnerAssignment.StatusReturned => "bg-secondary",
                    PartnerAssignment.StatusLate => "bg-danger",
                    _ => "bg-warning text-dark"
                };
                var label = effectiveStatus switch
                {
                    PartnerAssignment.StatusPending => "قيد المراجعة",
                    PartnerAssignment.StatusSubmitted => "تم الرد",
                    PartnerAssignment.StatusReturned => "مرتجَع",
                    PartnerAssignment.StatusLate => "متأخر",
                    _ => effectiveStatus
                };

                AssessmentHeader? assessment = null;
                if (assessmentByDept.TryGetValue(p.PartnerDepartmentId, out var a))
                {
                    assessment = a;
                }

                var hasResponse = assessment is not null;
                var responseComment = assessment?.Comment;
                var totalScore = assessment?.TotalScore;
                var submittedAt = assessment?.SubmittedAt;
                var scores = assessment is not null && scoresByHeader.TryGetValue(assessment.Id, out var s)
                    ? s
                    : new List<SpecializedPartnerScoreLineDto>();

                return new SpecializedPartnerFollowUpRowDto(
                    p.Id, p.InnovationIdeaId, header.ReferenceNumber, header.Title,
                    p.PartnerDepartmentName, label, badge,
                    p.SentAt, p.RespondedAt, daysOpen, isLate, p.Note,
                    hasResponse, responseComment, totalScore, submittedAt, scores);
            }).ToList();

            return new SpecializedPartnerOpinionDto(header.Id, header.ReferenceNumber, header.Title, items);
        }

        public async Task<SpecializedSendToCommitteeDto?> GetSendToCommitteeSummaryAsync(Guid ideaId, Guid departmentId, CancellationToken ct)
        {
            var header = await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.Id == ideaId && i.AssignedDepartmentId == departmentId)
                .Select(i => new { i.Id, i.ReferenceNumber })
                .FirstOrDefaultAsync(ct);

            if (header is null) return null;

            var criteriaCount = await _db.AssessmentCriteria.CountAsync(c => c.IsActive, ct);

            var latestSpecialized = await _db.AssessmentHeaders
                .AsNoTracking()
                .Where(h => h.InnovationIdeaId == ideaId
                    && h.AssessorDepartmentId == departmentId
                    && h.Source == AssessmentHeader.SourceSpecialized
                    && !h.IsDraft)
                .OrderByDescending(h => h.SubmittedAt ?? h.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var completedCriteria = latestSpecialized is null
                ? 0
                : await _db.AssessmentDetails.CountAsync(d => d.AssessmentHeaderId == latestSpecialized.Id, ct);

            var unreplied = await _db.PartnerAssignments.CountAsync(p =>
                p.InnovationIdeaId == ideaId
                && (p.Status == PartnerAssignment.StatusPending || p.Status == PartnerAssignment.StatusLate), ct);

            var canSend = completedCriteria >= criteriaCount && criteriaCount > 0;
            string? warning = null;
            if (canSend && unreplied > 0)
            {
                warning = $"لديك {unreplied} شريك لم يرد بعد. هل تريد الإرسال إلى اللجنة رغم ذلك؟";
            }

            return new SpecializedSendToCommitteeDto(
                header.Id, header.ReferenceNumber,
                criteriaCount, completedCriteria, unreplied,
                canSend, warning);
        }

        public async Task<AssessmentHeader?> GetDraftHeaderAsync(Guid ideaId, Guid departmentId, CancellationToken ct)
        {
            return await _db.AssessmentHeaders
                .Include(h => h.Details)
                .Where(h => h.InnovationIdeaId == ideaId
                    && h.AssessorDepartmentId == departmentId
                    && h.Source == AssessmentHeader.SourceSpecialized
                    && h.IsDraft
                    && !h.IsLocked)
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public Task<bool> HasLockedAssessmentAsync(Guid ideaId, Guid departmentId, CancellationToken ct)
            => _db.AssessmentHeaders
                .AsNoTracking()
                .AnyAsync(h => h.InnovationIdeaId == ideaId
                    && h.AssessorDepartmentId == departmentId
                    && h.Source == AssessmentHeader.SourceSpecialized
                    && h.IsLocked, ct);

        public Task<bool> HasPartnerAssignmentsAsync(Guid ideaId, CancellationToken ct)
            => _db.PartnerAssignments
                .AsNoTracking()
                .AnyAsync(p => p.InnovationIdeaId == ideaId, ct);

        public async Task<IReadOnlyList<AssessmentHeader>> GetSpecializedFinalHeadersAsync(Guid ideaId, CancellationToken ct)
        {
            return await _db.AssessmentHeaders
                .AsNoTracking()
                .Include(h => h.Details)
                .Where(h => h.InnovationIdeaId == ideaId
                    && h.Source == AssessmentHeader.SourceSpecialized
                    && !h.IsDraft)
                .ToListAsync(ct);
        }

        public async Task<InnovationIdea?> GetIdeaForDepartmentAsync(Guid ideaId, Guid departmentId, CancellationToken ct)
        {
            return await _db.InnovationIdeas
                .Include(i => i.CurrentStatus)
                .FirstOrDefaultAsync(i => i.Id == ideaId && i.AssignedDepartmentId == departmentId, ct);
        }

        public async Task<IReadOnlyList<PartnerAssignment>> GetPartnerAssignmentsForIdeaAsync(Guid ideaId, CancellationToken ct)
            => await _db.PartnerAssignments.Where(p => p.InnovationIdeaId == ideaId).ToListAsync(ct);

        public async Task AddPartnerAssignmentsAsync(IEnumerable<PartnerAssignment> rows, CancellationToken ct)
            => await _db.PartnerAssignments.AddRangeAsync(rows, ct);

        public async Task AddOrUpdateAssessmentHeaderAsync(AssessmentHeader header, CancellationToken ct)
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

        public async Task AddStatusHistoryAsync(IdeaStatusHistory history, CancellationToken ct)
            => await _db.IdeaStatusHistories.AddAsync(history, ct);

        public async Task AddAuditActionAsync(AuditActionItem action, CancellationToken ct)
            => await _db.AuditActionItems.AddAsync(action, ct);

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

        private sealed record SpecializedDetailsHeader(
            Guid Id,
            string Reference,
            string Title,
            string Description,
            string? ProblemStatement,
            string? ProposedSolution,
            string? ExpectedBenefits,
            string? DomainName,
            string? ExpectedImpactName,
            string? TargetAudienceName,
            string? ApplicantName,
            string? ApplicantDepartmentName,
            string StatusCode,
            string StatusName,
            string StatusColor,
            DateTime? SubmittedAt,
            DateTime? AssignedAt);

        private sealed record PartnerOpinionRowRaw(
            Guid Id,
            Guid InnovationIdeaId,
            Guid PartnerDepartmentId,
            string PartnerDepartmentName,
            string Status,
            DateTime SentAt,
            DateTime? RespondedAt,
            string? Note);
    }
}