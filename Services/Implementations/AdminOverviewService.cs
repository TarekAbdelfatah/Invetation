using Ibtikar.DTOs.AdminOverview;
using Ibtikar.Repositories;
using Ibtikar.Services.Interfaces;

namespace Ibtikar.Services.Implementations
{
    public sealed class AdminOverviewService : IAdminOverviewService
    {
        private readonly IAdminOverviewRepository _repo;

        public AdminOverviewService(IAdminOverviewRepository repo) => _repo = repo;

        public Task<AdminOverviewDto> GetSnapshotAsync(CancellationToken ct)
            => _repo.GetSnapshotAsync(ct);

        public Task<AdminOverviewListDto> GetIdeasAsync(string? statusFilter, int page, int pageSize, CancellationToken ct)
            => _repo.GetIdeasAsync(statusFilter, Math.Max(1, page), Math.Clamp(pageSize, 5, 50), ct);

        public Task<AdminOverviewDetailsDto?> GetDetailsAsync(Guid id, CancellationToken ct)
            => _repo.GetDetailsAsync(id, ct);
    }
}