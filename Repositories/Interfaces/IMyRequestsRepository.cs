using Ibtikar.DTOs.MyRequests;
using Ibtikar.Models;

namespace Ibtikar.Repositories
{
    public interface IMyRequestsRepository
    {
        Task<MyRequestsListDto> GetListAsync(Guid applicantId, int page, int pageSize, CancellationToken ct);
        Task<MyRequestDetailsDto?> GetDetailsAsync(Guid applicantId, Guid id, CancellationToken ct);
        Task<InnovationIdea?> GetForApplicantAsync(Guid applicantId, Guid id, CancellationToken ct);
        Task<Guid?> GetStatusIdByCodeAsync(string code, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}