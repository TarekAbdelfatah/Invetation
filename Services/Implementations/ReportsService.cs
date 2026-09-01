using Ibtikar.DTOs.Reports;
using Ibtikar.Repositories;

namespace Ibtikar.Services.Implementations
{
    public sealed class ReportsService : IReportsService
    {
        private readonly IReportsRepository _repo;

        public ReportsService(IReportsRepository repo) => _repo = repo;

        public Task<ReportsSummaryDto> GetSummaryAsync(DateTime from, DateTime to, CancellationToken ct)
            => _repo.GetSummaryAsync(from, to, ct);

        public Task<ReportsChallengesDto> GetChallengesAsync(DateTime from, DateTime to, Guid? domainId, int take, CancellationToken ct)
            => _repo.GetChallengesAsync(from, to, domainId, take, ct);
    }
}
