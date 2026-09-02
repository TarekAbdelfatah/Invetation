using Ibtikar.DTOs.MyRequests;

namespace Ibtikar.DTOs.Committee
{
    public sealed record CommitteeIdeaReadOnlyDto(
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
        IReadOnlyList<MyRequestAttachmentDto> Attachments);
}
