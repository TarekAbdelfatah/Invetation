namespace Ibtikar.DTOs.Ideas
{
    public sealed record CreateIdeaRequestDto(
        string Title,
        string Description,
        string? ProblemStatement,
        string? ProposedSolution,
        string? ExpectedBenefits,
        Guid? InnovationDomainId,
        Guid? ExpectedImpactId,
        string? ExpectedImpactOther,
        Guid? TargetAudienceId,
        string? TargetAudienceOther,
        bool UsesEmergingTech,
        IReadOnlyList<Guid> TechnologyIds,
        string? TechnologyOther,
        string? RequiredResources);
}