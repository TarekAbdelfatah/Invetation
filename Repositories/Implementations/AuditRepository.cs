using Ibtikar.Data;
using Ibtikar.DTOs.Audit;
using Ibtikar.Models;
using Ibtikar.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public sealed class AuditRepository : IAuditRepository
    {
        private readonly IbtikarDbContext _db;

        public AuditRepository(IbtikarDbContext db) => _db = db;

        public async Task<AuditInboxDto> GetInboxRowsAsync(
            string applicantTypeFilter,
            IReadOnlyList<string> statusCodes,
            int page,
            int pageSize,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var overdueThreshold = TimeSpan.FromHours(48);

            var query = _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => !i.IsDraft)
                .Where(i => _db.IdeaStatuses
                    .Where(s => statusCodes.Contains(s.Code))
                    .Select(s => s.Id)
                    .Contains(i.CurrentStatusId));

            query = applicantTypeFilter switch
            {
                "internal" => query.Where(i => i.ApplicantDepartmentId != null),
                "external" => query.Where(i => i.ApplicantDepartmentId == null),
                _ => query
            };

            var totalCount = await query.CountAsync(ct);

            var rows = await query
                .OrderByDescending(i => i.SubmittedAt ?? i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new AuditInboxRowDto(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.InnovationDomain != null ? i.InnovationDomain.Name : "—",
                    i.ApplicantUser != null ? i.ApplicantUser.FullName : "—",
                    i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : "—",
                    i.AssignedDepartment != null ? i.AssignedDepartment.Name : null,
                    i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d",
                    i.SubmittedAt ?? i.CreatedAt,
                    now - (i.SubmittedAt ?? i.CreatedAt) > overdueThreshold,
                    false,
                    null,
                    null))
                .ToListAsync(ct);

            if (rows.Count > 0)
            {
                var ideaIds = rows.Select(r => r.Id).ToList();
                var returns = await _db.AuditActionItems
                    .AsNoTracking()
                    .Where(a => a.Decision == "not_competent_return" && ideaIds.Contains(a.IdeaId))
                    .GroupBy(a => a.IdeaId)
                    .Select(g => new
                    {
                        IdeaId = g.Key,
                        Reason = g.OrderByDescending(x => x.AuditDate).First().DecisionText,
                        At = g.OrderByDescending(x => x.AuditDate).First().AuditDate,
                        DepartmentName = g.OrderByDescending(x => x.AuditDate).First().TargetDepartment != null
                            ? g.OrderByDescending(x => x.AuditDate).First().TargetDepartment!.Name
                            : null
                    })
                    .ToListAsync(ct);

                var returnMap = returns.ToDictionary(r => r.IdeaId);
                for (var i = 0; i < rows.Count; i++)
                {
                    if (returnMap.TryGetValue(rows[i].Id, out var info))
                    {
                        rows[i] = rows[i] with
                        {
                            IsReturnedBySpecialist = true,
                            ReturnedReason = info.Reason,
                            ReturnedAt = info.At
                        };
                    }
                }
            }

            return new AuditInboxDto(rows, applicantTypeFilter, string.Empty, page, pageSize, totalCount);
        }

        public async Task<AuditDetailsDto?> GetDetailsAsync(Guid id, CancellationToken ct)
        {
            var header = await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.Id == id && !i.IsDraft)
                .Select(i => new
                {
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.Description,
                    i.ProblemStatement,
                    i.ProposedSolution,
                    i.ExpectedBenefits,
                    i.RequiredResources,
                    i.ExpectedImpactOther,
                    i.TargetAudienceOther,
                    i.UsesEmergingTech,
                    i.TechnologyOther,
                    DomainName = i.InnovationDomain != null ? i.InnovationDomain.Name : "—",
                    ExpectedImpactName = i.ExpectedImpact != null ? i.ExpectedImpact.Name : null,
                    TargetAudienceName = i.TargetAudience != null ? i.TargetAudience.Name : null,
                    ApplicantName = i.ApplicantUser != null ? i.ApplicantUser.FullName : "—",
                    ApplicantDepartmentName = i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : "خارجي",
                    AssignedDepartmentName = i.AssignedDepartment != null ? i.AssignedDepartment.Name : null,
                    AssignedDepartmentId = i.AssignedDepartmentId,
                    StatusName = i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    StatusColor = i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d",
                    StatusCode = i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty,
                    IsTerminal = i.CurrentStatus != null && i.CurrentStatus.IsTerminal,
                    i.SubmittedAt,
                    i.CreatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (header is null) return null;

            var attachments = await _db.IdeaAttachments
                .AsNoTracking()
                .Where(a => a.InnovationIdeaId == id)
                .OrderBy(a => a.UploadedAt)
                .Select(a => new AuditAttachmentDto(
                    a.Id,
                    a.FileName,
                    a.SizeBytes,
                    a.ContentType,
                    a.UploadedAt))
                .ToListAsync(ct);

            var history = await _db.IdeaStatusHistories
                .AsNoTracking()
                .Where(h => h.InnovationIdeaId == id)
                .OrderByDescending(h => h.ChangedAt)
                .Take(20)
                .Select(h => new AuditHistoryRowDto(
                    h.ChangedAt,
                    h.FromStatus != null ? h.FromStatus.NameEn : "—",
                    h.ToStatus != null ? h.ToStatus.Name : "—",
                    h.ChangedBy != null ? h.ChangedBy.FullName : "—",
                    h.Note))
                .ToListAsync(ct);

            var departments = await _db.Departments
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .Select(d => new AuditDepartmentOptionDto(d.Id, d.Name))
                .ToListAsync(ct);

            var actionableStatuses = new[] { IdeaStatusCodes.New, IdeaStatusCodes.Resubmitted };
            var isUnderStudy = header.StatusCode == IdeaStatusCodes.UnderStudy;
            var isRoutedToSpecialist = isUnderStudy && header.AssignedDepartmentId.HasValue;
            var canDecide = actionableStatuses.Contains(header.StatusCode)
                || (isUnderStudy && !header.AssignedDepartmentId.HasValue);

            var latestCompletionNote = await _db.IdeaStatusHistories
                .AsNoTracking()
                .Where(h => h.InnovationIdeaId == id
                    && h.ToStatus != null
                    && h.ToStatus.Code == IdeaStatusCodes.WaitingForCompletion)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new { h.Note, h.ChangedAt })
                .FirstOrDefaultAsync(ct);

            var latestNotCompetent = await _db.AuditActionItems
                .AsNoTracking()
                .Where(a => a.IdeaId == id && a.Decision == "not_competent_return")
                .OrderByDescending(a => a.AuditDate)
                .Select(a => new
                {
                    a.DecisionText,
                    a.AuditDate,
                    DepartmentName = a.TargetDepartment != null ? a.TargetDepartment.Name : null
                })
                .FirstOrDefaultAsync(ct);

            return new AuditDetailsDto(
                header.Id,
                header.ReferenceNumber,
                header.Title,
                header.Description,
                header.ProblemStatement,
                header.ProposedSolution,
                header.ExpectedBenefits,
                header.RequiredResources,
                header.ExpectedImpactName,
                header.ExpectedImpactOther,
                header.TargetAudienceName,
                header.TargetAudienceOther,
                header.UsesEmergingTech,
                header.TechnologyOther,
                header.DomainName,
                header.ApplicantName,
                header.ApplicantDepartmentName,
                header.AssignedDepartmentName,
                header.StatusCode,
                header.StatusName,
                header.StatusColor,
                header.SubmittedAt ?? header.CreatedAt,
                canDecide,
                isUnderStudy,
                isRoutedToSpecialist,
                header.IsTerminal,
                latestCompletionNote?.Note,
                latestCompletionNote?.ChangedAt,
                latestNotCompetent?.DecisionText,
                latestNotCompetent?.DepartmentName,
                latestNotCompetent?.AuditDate,
                departments,
                history,
                attachments);
        }

        public async Task<InnovationIdea?> GetForTransitionAsync(Guid id, CancellationToken ct)
        {
            return await _db.InnovationIdeas
                .Include(i => i.CurrentStatus)
                .FirstOrDefaultAsync(i => i.Id == id, ct);
        }

        public async Task<Department?> GetActiveDepartmentAsync(Guid departmentId, CancellationToken ct)
            => await _db.Departments.FirstOrDefaultAsync(d => d.Id == departmentId && d.IsActive, ct);

        public async Task<Guid?> GetStatusIdByCodeAsync(string code, CancellationToken ct)
            => await _db.IdeaStatuses
                .AsNoTracking()
                .Where(s => s.Code == code)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);

        public async Task AddStatusHistoryAsync(IdeaStatusHistory history, CancellationToken ct)
            => await _db.IdeaStatusHistories.AddAsync(history, ct);

        public async Task AddAuditActionAsync(AuditActionItem action, CancellationToken ct)
            => await _db.AuditActionItems.AddAsync(action, ct);

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}