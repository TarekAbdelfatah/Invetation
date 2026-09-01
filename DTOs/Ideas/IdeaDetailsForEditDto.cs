namespace Ibtikar.DTOs.Ideas
{
    public sealed record IdeaDetailsForEditDto(
        Guid Id,
        string Title,
        string Description,
        string? ProblemStatement,
        string? ProposedSolution,
        string? ExpectedBenefits,
        string? RequiredResources,
        Guid? InnovationDomainId,
        Guid? ExpectedImpactId,
        string? ExpectedImpactOther,
        Guid? TargetAudienceId,
        string? TargetAudienceOther,
        bool UsesEmergingTech,
        IReadOnlyList<Guid> TechnologyIds,
        string? TechnologyOther);
}
