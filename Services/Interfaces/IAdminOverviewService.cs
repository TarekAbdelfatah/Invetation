using Ibtikar.DTOs.AdminOverview;

namespace Ibtikar.Services.Admin
{
    public interface IAdminOverviewService
    {
        Task<AdminOverviewDto> GetSnapshotAsync(CancellationToken ct);
    }
}