using Ibtikar.Models;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ibtikar.Services.Ideas
{
    public interface IIdeaService
    {
        Task<IdeaCreateOutcome> CreateIdeaAsync(
            IdeaCreateViewModel model,
            Guid userId,
            Guid? departmentId,
            bool isSaveDraft,
            List<IFormFile>? attachments,
            CancellationToken ct);

        Task<InnovationIdea?> GetByReferenceForUserAsync(string referenceNumber, Guid userId, CancellationToken ct);
        Task<IdeaSuccessVm?> GetSuccessVmByReferenceAsync(string referenceNumber, Guid userId, CancellationToken ct);
        Task<IReadOnlyList<InnovationIdea>> GetLatestAsync(int take, CancellationToken ct);

        Task<IdeaLookups> GetLookupsAsync(CancellationToken ct);

        Task<User?> GetUserWithDepartmentAsync(Guid userId, CancellationToken ct);
    }

    public sealed record IdeaLookups(
        IReadOnlyList<SelectListItem> InnovationDomains,
        IReadOnlyList<SelectListItem> ExpectedImpacts,
        IReadOnlyList<SelectListItem> TargetAudiences,
        IReadOnlyList<SelectListItem> Technologies);

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
