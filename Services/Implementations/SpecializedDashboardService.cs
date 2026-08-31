using Ibtikar.DTOs.SpecializedDashboard;
using Ibtikar.Repositories;

namespace Ibtikar.Services.SpecializedDashboard
{
    public sealed class SpecializedDashboardService : ISpecializedDashboardService
    {
        private readonly ISpecializedDashboardRepository _repo;

        public SpecializedDashboardService(ISpecializedDashboardRepository repo) => _repo = repo;

        public async Task<SpecializedDashboardDto?> GetSnapshotAsync(Guid? departmentId, CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty) return null;
            return await _repo.GetSnapshotAsync(departmentId.Value, ct);
        }
    }
}