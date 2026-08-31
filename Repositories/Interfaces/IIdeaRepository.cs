using Ibtikar.Models;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ibtikar.Repositories
{
    public interface IIdeaRepository
    {
        Task<IReadOnlyList<InnovationIdea>> GetLatestAsync(int take, CancellationToken ct);
        Task<InnovationIdea?> GetByReferenceForUserAsync(string referenceNumber, Guid userId, CancellationToken ct);
        Task<IdeaSuccessVm?> GetSuccessVmByReferenceAsync(string referenceNumber, Guid userId, CancellationToken ct);
        Task<IdeaStatus?> GetStatusByCodeAsync(string code, CancellationToken ct);
        Task<string> GenerateReferenceNumberAsync(CancellationToken ct);
        Task AddAsync(InnovationIdea idea, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);

        Task<User?> GetUserWithDepartmentAsync(Guid userId, CancellationToken ct);

        Task<IReadOnlyList<SelectListItem>> GetActiveDomainsAsync(CancellationToken ct);
        Task<IReadOnlyList<SelectListItem>> GetActiveImpactsAsync(CancellationToken ct);
        Task<IReadOnlyList<SelectListItem>> GetActiveAudiencesAsync(CancellationToken ct);
        Task<IReadOnlyList<SelectListItem>> GetActiveTechnologiesAsync(CancellationToken ct);
    }
}
