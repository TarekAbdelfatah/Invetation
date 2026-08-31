using Ibtikar.DTOs.SpecializedDashboard;

namespace Ibtikar.Services.SpecializedDashboard
{
    public interface ISpecializedDashboardService
    {
        Task<SpecializedDashboardDto?> GetSnapshotAsync(Guid? departmentId, CancellationToken ct);
    }
}