using Ibtikar.DTOs.AdminOverview;

namespace Ibtikar.Repositories
{
    public interface IAdminOverviewRepository
    {
        Task<AdminOverviewDto> GetSnapshotAsync(int recentTake, CancellationToken ct);
        Task<AdminOverviewListDto> GetIdeasAsync(string? statusFilter, int page, int pageSize, CancellationToken ct);
        Task<AdminOverviewDetailsDto?> GetDetailsAsync(Guid id, CancellationToken ct);
    }
}