using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ibtikar.DTOs.Ideas
{
    public sealed record IdeaLookupsDto(
        IReadOnlyList<SelectListItem> InnovationDomains,
        IReadOnlyList<SelectListItem> ExpectedImpacts,
        IReadOnlyList<SelectListItem> TargetAudiences,
        IReadOnlyList<SelectListItem> Technologies);
}