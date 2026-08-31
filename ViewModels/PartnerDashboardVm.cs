namespace Ibtikar.ViewModels
{
    public class PartnerDashboardVm
    {
        public int PendingAssignments { get; set; }
        public int OverdueLate { get; set; }
        public int SubmittedThisCycle { get; set; }
        public List<PartnerAssignmentRowVm> Items { get; set; } = new();
        public string? DepartmentName { get; set; }
    }

    public sealed record PartnerAssignmentRowVm(
        Guid AssignmentId,
        Guid IdeaId,
        string IdeaReference,
        string IdeaTitle,
        string ApplicantName,
        string SourceDepartmentName,
        DateTime SentAt,
        DateTime? RespondedAt,
        string Status,
        bool IsLate,
        bool IsPending,
        bool IsReturned,
        double DaysOpen);

    public class PartnerDetailsVm
    {
        public Guid AssignmentId { get; set; }
        public Guid IdeaId { get; set; }
        public string IdeaReference { get; set; } = string.Empty;
        public string IdeaTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ProblemStatement { get; set; }
        public string? ProposedSolution { get; set; }
        public string? ExpectedBenefits { get; set; }
        public string? DomainName { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantDepartmentName { get; set; } = string.Empty;
        public string SourceDepartmentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public bool CanScore { get; set; }
        public bool AlreadyScored { get; set; }
        public List<PartnerCriterionVm> Criteria { get; set; } = new();
        public List<PartnerScoreLineVm> ExistingScores { get; set; } = new();
        public decimal? TotalScore { get; set; }
        public string? Comment { get; set; }
        public bool IsNotCompetentReturn { get; set; }
        public string? NotCompetentReason { get; set; }
        public bool CanReturnNotCompetent { get; set; }
    }

    public sealed record PartnerCriterionVm(Guid Id, string Code, string Name, int DisplayOrder);
    public sealed record PartnerScoreLineVm(Guid CriterionId, string CriterionCode, string CriterionName, int? Score, string? Comment);
}