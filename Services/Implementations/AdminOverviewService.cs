using Ibtikar.DTOs.AdminOverview;
using Ibtikar.Repositories;

namespace Ibtikar.Services.Admin
{
    public sealed class AdminOverviewService : IAdminOverviewService
    {
        private const int RecentTake = 8;
        private const int IdeasTake = 200;

        private readonly IAdminOverviewRepository _repo;

        public AdminOverviewService(IAdminOverviewRepository repo) => _repo = repo;

        public Task<AdminOverviewDto> GetSnapshotAsync(CancellationToken ct)
            => _repo.GetSnapshotAsync(RecentTake, ct);

        public Task<AdminOverviewListDto> GetIdeasAsync(string? statusFilter, int take, CancellationToken ct)
            => _repo.GetIdeasAsync(statusFilter, Math.Clamp(take, 10, 500), ct);
    }
}