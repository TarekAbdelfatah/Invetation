namespace Ibtikar.ViewModels
{
    public record MyRequestVm(
        Guid Id,
        string Reference,
        string Title,
        bool IsDraft,
        string StatusCode,
        string StatusName,
        string StatusColor,
        DateTime CreatedAt,
        DateTime? SubmittedAt);

    public class MyRequestsVm
    {
        public List<MyRequestVm> Items { get; }
        public MyRequestsVm(List<MyRequestVm> items) => Items = items;
    }

    public record MyRequestDetailsVm(
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
        List<MyRequestAttachmentVm> Attachments);

    public record MyRequestAttachmentVm(Guid Id, string FileName, long SizeBytes, DateTime UploadedAt);
}