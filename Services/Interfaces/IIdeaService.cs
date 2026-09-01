using Ibtikar.DTOs.Ideas;
using Microsoft.AspNetCore.Http;

namespace Ibtikar.Services.Interfaces
{
    public interface IIdeaService
    {
        Task<IdeaCreateOutcome> CreateIdeaAsync(
            CreateIdeaRequestDto request,
            Guid userId,
            Guid? departmentId,
            bool isSaveDraft,
            Guid? draftId,
            Guid? existingDraftId,
            List<IFormFile>? attachments,
            CancellationToken ct);

        Task<IdeaDetailsDto?> GetDetailsAsync(string referenceNumber, Guid userId, CancellationToken ct);
        Task<IReadOnlyList<IdeaSummaryDto>> GetLatestAsync(int take, CancellationToken ct);

        Task<IdeaLookupsDto> GetLookupsAsync(CancellationToken ct);

        Task<UserSummaryDto?> GetUserSummaryAsync(Guid userId, CancellationToken ct);

        Task<IdeaDetailsForEditDto?> GetDraftForEditAsync(Guid ideaId, Guid applicantId, IReadOnlyList<Guid> technologyIds, CancellationToken ct);
    }

    public sealed record IdeaCreateOutcome(
        bool Success,
        bool IsSubmitted,
        string? ReferenceNumber,
        IReadOnlyList<string> Errors)
    {
        public static IdeaCreateOutcome Failed(IReadOnlyList<string> errors)
            => new(false, false, null, errors);

        public static IdeaCreateOutcome Failed(string error)
            => new(false, false, null, new[] { error });

        public static IdeaCreateOutcome Submitted(string referenceNumber)
            => new(true, true, referenceNumber, Array.Empty<string>());

        public static IdeaCreateOutcome DraftSaved()
            => new(true, false, null, Array.Empty<string>());
    }
}