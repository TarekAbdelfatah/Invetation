using Ibtikar.DTOs.Ideas;
using Ibtikar.Models;

namespace Ibtikar.Repositories
{
    public interface IIdeaRepository
    {
        Task<IReadOnlyList<IdeaSummaryDto>> GetLatestAsync(int take, CancellationToken ct);
        Task<IdeaDetailsDto?> GetDetailsAsync(string referenceNumber, Guid userId, CancellationToken ct);
        Task<IdeaStatus?> GetStatusByCodeAsync(string code, CancellationToken ct);
        Task<string> GenerateReferenceNumberAsync(CancellationToken ct);
        Task AddAsync(InnovationIdea idea, CancellationToken ct);
        Task<InnovationIdea?> GetDraftByIdAsync(Guid ideaId, Guid applicantId, CancellationToken ct);
        Task<IReadOnlyList<Guid>> GetDraftTechnologyIdsAsync(Guid ideaId, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);

        Task<UserSummaryDto?> GetUserSummaryAsync(Guid userId, CancellationToken ct);

        Task<IdeaLookupsDto> GetLookupsAsync(CancellationToken ct);
    }
}