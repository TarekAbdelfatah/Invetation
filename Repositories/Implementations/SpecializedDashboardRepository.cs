using Ibtikar.Data;
using Ibtikar.DTOs.SpecializedDashboard;
using Ibtikar.Services.Ideas;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public sealed class SpecializedDashboardRepository : ISpecializedDashboardRepository
    {
        private readonly IbtikarDbContext _db;

        public SpecializedDashboardRepository(IbtikarDbContext db) => _db = db;

        public async Task<SpecializedDashboardDto> GetSnapshotAsync(Guid departmentId, CancellationToken ct)
        {
            var routed = _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.AssignedDepartmentId == departmentId);

            var underStudy = await routed
                .Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.UnderStudy)
                .CountAsync(ct);

            var sentToExecution = await routed
                .Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.InExecution)
                .CountAsync(ct);

            var rejectedAfterRouting = await routed
                .Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.Rejected)
                .CountAsync(ct);

            return new SpecializedDashboardDto(underStudy, 0, sentToExecution, rejectedAfterRouting);
        }
    }
}