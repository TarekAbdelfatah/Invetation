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

        public async Task<IReadOnlyList<AuditInboxRowDto>> GetInboxRowsAsync(
            string applicantTypeFilter,
            IReadOnlyList<string> statusCodes,
            int take,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var overdueThreshold = TimeSpan.FromHours(48);

            var query = _db.InnovationIdeas
                .AsNoTracking()
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

            return await query
                .OrderByDescending(i => i.CreatedAt)
                .Take(take)
                .Select(i => new AuditInboxRowDto(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.InnovationDomain != null ? i.InnovationDomain.Name : "—",
                    i.ApplicantUser != null ? i.ApplicantUser.FullName : "—",
                    i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : "—",
                    i.CreatedAt,
                    now - i.CreatedAt > overdueThreshold))
                .ToListAsync(ct);
        }

        public async Task<AuditDetailsDto?> GetDetailsAsync(Guid id, CancellationToken ct)
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
                    ApplicantName = i.ApplicantUser != null ? i.ApplicantUser.FullName : "—",
                    ApplicantDepartmentName = i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : "خارجي",
                    AssignedDepartmentName = i.AssignedDepartment != null ? i.AssignedDepartment.Name : null,
                    StatusName = i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    StatusColor = i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d",
                    StatusCode = i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty,
                    IsTerminal = i.CurrentStatus != null && i.CurrentStatus.IsTerminal,
                    i.SubmittedAt,
                    i.CreatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (header is null) return null;

            var history = await _db.IdeaStatusHistories
                .AsNoTracking()
                .Where(h => h.InnovationIdeaId == id)
                .OrderByDescending(h => h.ChangedAt)
                .Take(10)
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

            var openStatuses = new[] { IdeaStatusCodes.New, IdeaStatusCodes.Resubmitted };
            var canOpen = openStatuses.Contains(header.StatusCode);
            var isUnderStudy = header.StatusCode == IdeaStatusCodes.UnderStudy;

            return new AuditDetailsDto(
                header.Id,
                header.ReferenceNumber,
                header.Title,
                header.Description,
                header.ProblemStatement,
                header.ProposedSolution,
                header.ExpectedBenefits,
                header.DomainName,
                header.ApplicantName,
                header.ApplicantDepartmentName,
                header.AssignedDepartmentName,
                header.StatusName,
                header.StatusColor,
                header.SubmittedAt ?? header.CreatedAt,
                canOpen,
                isUnderStudy,
                header.IsTerminal,
                departments,
                history);
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