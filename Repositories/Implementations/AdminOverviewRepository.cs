using Ibtikar.Data;
using Ibtikar.DTOs.AdminOverview;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public sealed class AdminOverviewRepository : IAdminOverviewRepository
    {
        private readonly IbtikarDbContext _db;

        public AdminOverviewRepository(IbtikarDbContext db) => _db = db;

        public async Task<AdminOverviewDto> GetSnapshotAsync(int recentTake, CancellationToken ct)
        {
            var totalIdeas = await _db.InnovationIdeas.CountAsync(ct);
            var drafts = await _db.InnovationIdeas.CountAsync(i => i.IsDraft, ct);
            var submitted = await _db.InnovationIdeas.CountAsync(i => !i.IsDraft, ct);
            var totalUsers = await _db.Users.CountAsync(u => u.IsActive, ct);

            var byStatus = await _db.IdeaStatuses
                .AsNoTracking()
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new AdminOverviewStatusCountDto(
                    s.Code,
                    s.Name,
                    s.Color,
                    _db.InnovationIdeas.Count(i => i.CurrentStatusId == s.Id)))
                .ToListAsync(ct);

            var recent = await _db.InnovationIdeas
                .AsNoTracking()
                .OrderByDescending(i => i.CreatedAt)
                .Take(recentTake)
                .Select(i => new AdminOverviewRecentIdeaDto(
                    i.ReferenceNumber,
                    i.Title,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#888",
                    i.InnovationDomain != null ? i.InnovationDomain.Name : "—",
                    i.CreatedAt))
                .ToListAsync(ct);

            return new AdminOverviewDto(totalIdeas, drafts, submitted, totalUsers, byStatus, recent);
        }

        public async Task<AdminOverviewListDto> GetIdeasAsync(string? statusFilter, int take, CancellationToken ct)
        {
            var query = _db.InnovationIdeas.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(statusFilter))
                query = query.Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == statusFilter);

            var total = await query.CountAsync(ct);
            var rows = await query
                .OrderByDescending(i => i.CreatedAt)
                .Take(take)
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
    }
}