namespace Ibtikar.DTOs.MyRequests
{
    public sealed record MyRequestDetailsDto(
        Guid Id,
        string Reference,
        string Title,
        string Description,
        string? ProblemStatement,
        string? ProposedSolution,
        string? ExpectedBenefits,
        string? ExpectedImpactOther,
        string? TargetAudienceOther,
        bool UsesEmergingTech,
        string? TechnologyOther,
        string StatusCode,
        string StatusName,
        string StatusColor,
        string? DomainName,
        string? ExpectedImpactName,
        string? TargetAudienceName,
        DateTime CreatedAt,
        DateTime? SubmittedAt,
        string? CompletionNotes,
        string? DevelopmentNotes,
        string? RejectionReason,
        IReadOnlyList<MyRequestAttachmentDto> Attachments);

    public sealed record MyRequestAttachmentDto(Guid Id, string FileName, long SizeBytes, DateTime UploadedAt);

    public sealed record MyRequestContentUpdateDto(
        string Description,
        string? ProblemStatement,
        string? ProposedSolution,
        string? ExpectedBenefits);
}