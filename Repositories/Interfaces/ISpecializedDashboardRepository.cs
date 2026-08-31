using Ibtikar.DTOs.SpecializedDashboard;

namespace Ibtikar.Repositories
{
    public interface ISpecializedDashboardRepository
    {
        Task<SpecializedDashboardDto> GetSnapshotAsync(Guid departmentId, CancellationToken ct);
    }
}