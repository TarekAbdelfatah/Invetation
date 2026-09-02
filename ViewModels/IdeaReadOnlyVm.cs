namespace Ibtikar.ViewModels
{
    public sealed record IdeaReadOnlyVm(
        string Title,
        string Description,
        string? ProblemStatement,
        string? ProposedSolution,
        string? ExpectedBenefits,
        string? RequiredResources,
        string? DomainName,
        string? ExpectedImpactName,
        string? ExpectedImpactOther,
        string? TargetAudienceName,
        string? TargetAudienceOther,
        bool UsesEmergingTech,
        string? TechnologyOther,
        DateTime CreatedAt,
        DateTime? SubmittedAt,
        List<MyRequestAttachmentVm> Attachments);
}
