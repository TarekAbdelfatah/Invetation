using Ibtikar.DTOs.AdminOverview;

namespace Ibtikar.Services.Interfaces
{
    public interface IAdminOverviewService
    {
        Task<AdminOverviewDto> GetSnapshotAsync(CancellationToken ct);
        Task<AdminOverviewListDto> GetIdeasAsync(string? statusFilter, int page, int pageSize, CancellationToken ct);
        Task<AdminOverviewDetailsDto?> GetDetailsAsync(Guid id, CancellationToken ct);
    }
}