namespace Ibtikar.ViewModels
{
    public class CommitteeDashboardVm
    {
        public int UnderStudy { get; set; }
        public int UnderVoting { get; set; }
        public int Accepted { get; set; }
        public int Rejected { get; set; }
        public string? CommitteeName { get; set; }
    }

    public sealed record CommitteeReferralRowVm(
        Guid IdeaId,
        string Reference,
        string Title,
        string StatusCode,
        string StatusName,
        string StatusColor,
        string? ApplicantName,
        string? ApplicantDepartmentName,
        DateTime? ReferredAt,
        double StayDays,
        bool IsOverdue);

    public class CommitteeReferralsVm
    {
        public List<CommitteeReferralRowVm> Items { get; set; } = new();
        public string StatusFilter { get; set; } = string.Empty;
        public string? CommitteeName { get; set; }
    }

    public class CommitteeAssessVm
    {
        public Guid IdeaId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public string StatusColor { get; set; } = "#6c757d";
        public bool IsDraftSaved { get; set; }
        public bool IsLocked { get; set; }
        public Guid? DraftHeaderId { get; set; }
        public DateTime? DraftSavedAt { get; set; }
        public List<CommitteeCriterionVm> Criteria { get; set; } = new();
        public List<CommitteeAssessLineVm> Lines { get; set; } = new();
        public decimal? TotalScore { get; set; }
        public string? Comment { get; set; }
        public int? DepartmentPercent { get; set; }
        public int? CommitteePercent { get; set; }
        public int? CombinedAverage { get; set; }
    }

    public sealed record CommitteeCriterionVm(Guid Id, string Code, string Name, string? Description, int DisplayOrder);
    public sealed record CommitteeAssessLineVm(Guid CriterionId, string CriterionCode, string CriterionName, int? Score, string? Comment);

    public sealed record CommitteeVoteRowVm(
        Guid IdeaId,
        string Reference,
        string Title,
        string StatusCode,
        string StatusName,
        string StatusColor,
        bool HasVoted,
        string? MyVote);

    public class CommitteeVotesVm
    {
        public List<CommitteeVoteRowVm> Items { get; set; } = new();
    }

    public class CommitteeDecisionVm
    {
        public Guid IdeaId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int? CombinedAverage { get; set; }
        public bool CanAccept { get; set; }
        public string? ExtraConfirmWarning { get; set; }
    }
}
