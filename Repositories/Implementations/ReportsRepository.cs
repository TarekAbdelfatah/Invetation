using Ibtikar.Data;
using Ibtikar.DTOs.Reports;
using Ibtikar.Services.Ideas;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public sealed class ReportsRepository : IReportsRepository
    {
        private readonly IbtikarDbContext _db;

        public ReportsRepository(IbtikarDbContext db) => _db = db;

        public async Task<ReportsSummaryDto> GetSummaryAsync(DateTime from, DateTime to, CancellationToken ct)
        {
            var (fromUtc, toUtc) = NormalizeRange(from, to);

            var baseQuery = _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.CreatedAt >= fromUtc && i.CreatedAt < toUtc);

            var total = await baseQuery.CountAsync(ct);
            var submitted = await baseQuery.CountAsync(i => !i.IsDraft, ct);

            var approvedId = await _db.IdeaStatuses
                .AsNoTracking()
                .Where(s => s.Code == IdeaStatusCodes.Approved)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(ct);
            var inExecutionId = await _db.IdeaStatuses
                .AsNoTracking()
                .Where(s => s.Code == IdeaStatusCodes.InExecution)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(ct);
            var completedId = await _db.IdeaStatuses
                .AsNoTracking()
                .Where(s => s.Code == IdeaStatusCodes.Completed)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(ct);

            var approved = approvedId == Guid.Empty ? 0 : await baseQuery.CountAsync(i => i.CurrentStatusId == approvedId, ct);
            var inExec = inExecutionId == Guid.Empty ? 0 : await baseQuery.CountAsync(i => i.CurrentStatusId == inExecutionId, ct);
            var completed = completedId == Guid.Empty ? 0 : await baseQuery.CountAsync(i => i.CurrentStatusId == completedId, ct);

            var statusRows = await _db.IdeaStatuses
                .AsNoTracking()
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new
                {
                    s.Code,
                    s.Name,
                    s.Color,
                    Count = _db.InnovationIdeas.Count(i =>
                        i.CreatedAt >= fromUtc && i.CreatedAt < toUtc && i.CurrentStatusId == s.Id)
                })
                .ToListAsync(ct);

            var stageMix = statusRows
                .Select(r => new ReportsStageMixRowDto(
                    r.Code,
                    r.Name,
                    r.Color,
                    r.Count,
                    total == 0 ? 0d : Math.Round(100.0 * r.Count / total, 2)))
                .ToList();

            var sumOfCounts = stageMix.Sum(s => s.Count);
            string? warning = sumOfCounts != total
                ? $"مجموع عدد الحالات ({sumOfCounts}) لا يساوي الإجمالي ({total})."
                : null;

            return new ReportsSummaryDto(
                new ReportsKpiDto(total, submitted, approved, inExec, completed),
                stageMix,
                new ReportsDateRangeDto(fromUtc, toUtc),
                total == 0,
                warning);
        }

        public async Task<ReportsChallengesDto> GetChallengesAsync(DateTime from, DateTime to, Guid? domainId, int take, CancellationToken ct)
        {
            var (fromUtc, toUtc) = NormalizeRange(from, to);

            var rejectedCode = IdeaStatusCodes.Rejected;

            var query = _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.CreatedAt >= fromUtc && i.CreatedAt < toUtc)
                .Where(i => i.CurrentStatus == null || i.CurrentStatus.Code != rejectedCode);

            if (domainId.HasValue)
                query = query.Where(i => i.InnovationDomainId == domainId.Value);

            var rows = await query
                .OrderByDescending(i => i.CreatedAt)
                .Take(take)
                .Select(i => new ReportsChallengeRowDto(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.InnovationDomain != null ? i.InnovationDomain.Name : "—",
                    i.ApplicantUser != null ? i.ApplicantUser.FullName : "—",
                    i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : "خارجي",
                    i.ProblemStatement ?? "—",
                    i.ProposedSolution,
                    i.CreatedAt,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d"))
                .ToListAsync(ct);

            var totalCount = await query.CountAsync(ct);

            var domains = await _db.InnovationDomains
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .ThenBy(d => d.Name)
                .Select(d => new ReportsDomainOptionDto(d.Id, d.Name))
                .ToListAsync(ct);

            return new ReportsChallengesDto(
                rows,
                domains,
                domainId,
                new ReportsDateRangeDto(fromUtc, toUtc),
                totalCount);
        }

        private static (DateTime FromUtc, DateTime ToUtc) NormalizeRange(DateTime from, DateTime to)
        {
            var fromUtc = from.Kind == DateTimeKind.Utc ? from : DateTime.SpecifyKind(from.ToUniversalTime(), DateTimeKind.Utc);
            fromUtc = new DateTime(fromUtc.Year, fromUtc.Month, fromUtc.Day, 0, 0, 0, DateTimeKind.Utc);

            var toRaw = to.Kind == DateTimeKind.Utc ? to : DateTime.SpecifyKind(to.ToUniversalTime(), DateTimeKind.Utc);
            var toUtc = new DateTime(toRaw.Year, toRaw.Month, toRaw.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);

            return (fromUtc, toUtc);
        }
    }
}
