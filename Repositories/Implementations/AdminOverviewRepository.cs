using Ibtikar.Data;
using Ibtikar.DTOs.AdminOverview;
using Ibtikar.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public sealed class AdminOverviewRepository : IAdminOverviewRepository
    {
        private readonly IbtikarDbContext _db;

        public AdminOverviewRepository(IbtikarDbContext db) => _db = db;

        public async Task<AdminOverviewDto> GetSnapshotAsync(CancellationToken ct)
        {
            var totalIdeas = await _db.InnovationIdeas.CountAsync(ct);
            var drafts = await _db.InnovationIdeas.CountAsync(i => i.IsDraft, ct);
            var submitted = await _db.InnovationIdeas.CountAsync(i => !i.IsDraft, ct);
            var totalUsers = await _db.Users.CountAsync(u => u.IsActive, ct);

            var byStatus = await _db.IdeaStatuses
                .AsNoTracking()
                .Where(s => RecentAfterAuditCodes.Contains(s.Code))
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new AdminOverviewStatusCountDto(
                    s.Code,
                    s.Name,
                    s.Color,
                    _db.InnovationIdeas.Count(i => !i.IsDraft && i.CurrentStatusId == s.Id)))
                .ToListAsync(ct);

            return new AdminOverviewDto(totalIdeas, drafts, submitted, totalUsers, byStatus);
        }

        private static readonly HashSet<string> RecentAfterAuditCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            IdeaStatusCodes.UnderStudy,
            IdeaStatusCodes.UnderAssessment,
            IdeaStatusCodes.ReferredCommittee,
            IdeaStatusCodes.Approved,
            IdeaStatusCodes.Rejected,
            IdeaStatusCodes.InExecution,
            IdeaStatusCodes.Completed,
            IdeaStatusCodes.Cancelled,
            IdeaStatusCodes.ReturnedForDevelopment
        };

        public async Task<AdminOverviewListDto> GetIdeasAsync(string? statusFilter, int page, int pageSize, CancellationToken ct)
        {
            var query = _db.InnovationIdeas.AsNoTracking()
                .Where(i => !i.IsDraft
                    && i.CurrentStatus != null
                    && RecentAfterAuditCodes.Contains(i.CurrentStatus.Code));

            if (!string.IsNullOrWhiteSpace(statusFilter))
                query = query.Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == statusFilter);

            var total = await query.CountAsync(ct);
            var rows = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new AdminOverviewIdeaRowDto(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.InnovationDomain != null ? i.InnovationDomain.Name : "—",
                    i.ApplicantUser != null ? i.ApplicantUser.FullName : "—",
                    i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : "خارجي",
                    i.AssignedDepartment != null ? i.AssignedDepartment.Name : null,
                    i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d",
                    i.CreatedAt,
                    i.IsDraft))
                .ToListAsync(ct);

            return new AdminOverviewListDto(rows, statusFilter, total);
        }

        public async Task<AdminOverviewDetailsDto?> GetDetailsAsync(Guid id, CancellationToken ct)
        {
            var header = await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.Id == id)
                .Select(i => new
                {
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.Description,
                    i.ProblemStatement,
                    i.ProposedSolution,
                    i.ExpectedBenefits,
                    DomainName = i.InnovationDomain != null ? i.InnovationDomain.Name : "—",
                    ExpectedImpactName = i.ExpectedImpact != null ? i.ExpectedImpact.Name : "—",
                    TargetAudienceName = i.TargetAudience != null ? i.TargetAudience.Name : "—",
                    ApplicantName = i.ApplicantUser != null ? i.ApplicantUser.FullName : "—",
                    ApplicantDeptName = i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : "خارجي",
                    AssignedDeptName = i.AssignedDepartment != null ? i.AssignedDepartment.Name : null,
                    StatusName = i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    StatusColor = i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d",
                    StatusCode = i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty,
                    i.IsDraft,
                    i.SubmittedAt,
                    i.CreatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (header is null) return null;

            var attachments = await _db.IdeaAttachments
                .AsNoTracking()
                .Where(a => a.InnovationIdeaId == id)
                .OrderBy(a => a.UploadedAt)
                .Select(a => new AdminOverviewAttachmentDto(
                    a.Id,
                    a.FileName,
                    a.SizeBytes,
                    a.UploadedAt,
                    a.UploadedBy != null ? a.UploadedBy.FullName : null))
                .ToListAsync(ct);

            var assessments = await _db.AssessmentHeaders
                .AsNoTracking()
                .Where(h => h.InnovationIdeaId == id)
                .OrderByDescending(h => h.SubmittedAt ?? h.CreatedAt)
                .Select(h => new
                {
                    h.Id,
                    h.Source,
                    h.IsDraft,
                    h.IsLocked,
                    h.SubmittedAt,
                    h.TotalScore,
                    h.Comment,
                    AssessorName = h.Assessor != null ? h.Assessor.FullName : "—",
                    DepartmentName = h.AssessorDepartment != null ? h.AssessorDepartment.Name : "—",
                    Lines = h.Details.Select(d => new AdminOverviewAssessmentLineDto(
                        d.CriterionId,
                        d.Criterion != null ? d.Criterion.Code : "—",
                        d.Criterion != null ? d.Criterion.Name : "—",
                        d.Score,
                        d.Comment)).ToList()
                })
                .ToListAsync(ct);

            var timeline = await _db.IdeaStatusHistories
                .AsNoTracking()
                .Where(h => h.InnovationIdeaId == id)
                .OrderByDescending(h => h.ChangedAt)
                .Take(50)
                .Select(h => new AdminOverviewTimelineRowDto(
                    h.ChangedAt,
                    h.FromStatus != null ? h.FromStatus.NameEn ?? h.FromStatus.Name : "—",
                    h.ToStatus != null ? h.ToStatus.Name : "—",
                    h.ChangedBy != null ? h.ChangedBy.FullName : "—",
                    h.Note))
                .ToListAsync(ct);

            var dtos = assessments.Select(a => new AdminOverviewAssessmentDto(
                a.Id,
                a.Source,
                SourceLabel(a.Source),
                a.AssessorName,
                a.DepartmentName,
                a.IsDraft,
                a.IsLocked,
                a.SubmittedAt,
                a.TotalScore,
                a.Comment,
                a.Lines)).ToList();

            return new AdminOverviewDetailsDto(
                header.Id,
                header.ReferenceNumber,
                header.Title,
                header.Description,
                header.ProblemStatement,
                header.ProposedSolution,
                header.ExpectedBenefits,
                header.DomainName,
                header.ExpectedImpactName,
                header.TargetAudienceName,
                header.ApplicantName,
                header.ApplicantDeptName,
                header.AssignedDeptName,
                header.StatusName,
                header.StatusColor,
                header.StatusCode,
                header.IsDraft,
                header.SubmittedAt,
                header.CreatedAt,
                attachments,
                dtos,
                timeline);
        }

        private static string SourceLabel(string source) => source switch
        {
            "specialized" => "الإدارة المختصة",
            "partner" => "الإدارة الشريكة",
            "committee" => "اللجنة",
            _ => source
        };
    }
}