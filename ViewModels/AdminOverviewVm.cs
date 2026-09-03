namespace Ibtikar.ViewModels
{
    public class AdminOverviewVm
    {
        public int TotalIdeas { get; set; }
        public int Drafts { get; set; }
        public int Submitted { get; set; }
        public int TotalUsers { get; set; }
        public List<StatusCount> ByStatus { get; set; } = new();
        public string? StatusFilter { get; set; }
        public List<IdeaRow> Ideas { get; set; } = new();
        public int IdeasTotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public record StatusCount(string Code, string Name, string Color, int Count);
        public record IdeaRow(
            Guid Id,
            string Reference,
            string Title,
            string DomainName,
            string ApplicantName,
            string ApplicantDepartmentName,
            string? AssignedDepartmentName,
            string StatusCode,
            string StatusName,
            string StatusColor,
            DateTime CreatedAt,
            bool IsDraft);
    }

    public class AdminOverviewDetailsVm
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ProblemStatement { get; set; }
        public string? ProposedSolution { get; set; }
        public string? ExpectedBenefits { get; set; }
        public string DomainName { get; set; } = string.Empty;
        public string ExpectedImpactName { get; set; } = string.Empty;
        public string TargetAudienceName { get; set; } = string.Empty;
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantDepartmentName { get; set; } = string.Empty;
        public string? AssignedDepartmentName { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "#6c757d";
        public string StatusCode { get; set; } = string.Empty;
        public bool IsDraft { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AdminOverviewAttachmentVm> Attachments { get; set; } = new();
        public List<AdminOverviewAssessmentVm> Assessments { get; set; } = new();
        public List<AdminOverviewTimelineRowVm> Timeline { get; set; } = new();
    }

    public sealed record AdminOverviewAttachmentVm(
        Guid Id,
        string FileName,
        long SizeBytes,
        DateTime UploadedAt,
        string? UploadedByName);

    public sealed record AdminOverviewAssessmentLineVm(
        Guid CriterionId,
        string CriterionCode,
        string CriterionName,
        int Score,
        string? Comment);

    public sealed record AdminOverviewAssessmentVm(
        Guid Id,
        string Source,
        string SourceLabel,
        string AssessorName,
        string DepartmentName,
        bool IsDraft,
        bool IsLocked,
        DateTime? SubmittedAt,
        decimal? TotalScore,
        string? Comment,
        List<AdminOverviewAssessmentLineVm> Lines);

    public sealed record AdminOverviewTimelineRowVm(
        DateTime ChangedAt,
        string FromStatus,
        string ToStatus,
        string By,
        string? Note);
}