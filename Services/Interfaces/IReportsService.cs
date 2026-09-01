using Ibtikar.DTOs.Reports;

namespace Ibtikar.Services
{
    public interface IReportsService
    {
        Task<ReportsSummaryDto> GetSummaryAsync(DateTime from, DateTime to, CancellationToken ct);
        Task<ReportsChallengesDto> GetChallengesAsync(DateTime from, DateTime to, Guid? domainId, int take, CancellationToken ct);
    }
}
