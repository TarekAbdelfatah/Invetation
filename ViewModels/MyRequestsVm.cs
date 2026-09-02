using System.ComponentModel.DataAnnotations;

namespace Ibtikar.ViewModels
{
    public record MyRequestVm(
        Guid Id,
        string Reference,
        string Title,
        string TitleDisplay,
        string? DomainName,
        bool IsDraft,
        string StatusCode,
        string StatusName,
        string StatusColor,
        DateTime CreatedAt,
        DateTime? SubmittedAt);

    public sealed class MyRequestsVm
    {
        public List<MyRequestVm> Items { get; }
        public int Page { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
        public MyRequestsVm(List<MyRequestVm> items, int page, int pageSize, int totalCount)
        {
            Items = items;
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;
        }
    }

    public record MyRequestDetailsVm(
        Guid Id,
        string Reference,
        string Title,
        string Description,
        string? ProblemStatement,
        string? ProposedSolution,
        string? ExpectedBenefits,
        string? RequiredResources,
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

    public sealed class MyRequestResubmitVm
    {
        [Required(ErrorMessage = "وصف الفكرة مطلوب عند إعادة التقديم.")]
        public string? Description { get; set; }

        public string? ProblemStatement { get; set; }
        public string? ProposedSolution { get; set; }
        public string? ExpectedBenefits { get; set; }
    }
}