using System.ComponentModel.DataAnnotations;
using Ibtikar.Validation;

namespace Ibtikar.ViewModels
{
    public class CommitteeDashboardVm
    {
        public int UnderStudy { get; set; }
        public int UnderVoting { get; set; }
        public int Accepted { get; set; }
        public int Rejected { get; set; }
        public string? CommitteeName { get; set; }
        public bool IsHead { get; set; }
        public List<CommitteeReferralRowVm> Items { get; set; } = new();
    }

    public sealed record CommitteeReferralRowVm(
        Guid IdeaId,
        string Reference,
        string Title,
        string TitleDisplay,
        string StatusCode,
        string StatusName,
        string StatusColor,
        string? ApplicantName,
        string? ApplicantDepartmentName,
        DateTime? ReferredAt,
        double StayDays,
        bool IsOverdue,
        int? DepartmentPercent = null,
        int? CommitteePercent = null,
        int? MyCommitteePercent = null,
        bool HasAddedCommitteeAssessment = false);

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
        public IdeaReadOnlyVm? Idea { get; set; }
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
        string? MyVote,
        string Description,
        string? ProblemStatement,
        string? ProposedSolution,
        string? ExpectedBenefits,
        IdeaReadOnlyVm Idea);

    public class CommitteeVotesVm
    {
        public List<CommitteeVoteRowVm> Items { get; set; } = new();
        public bool IsHead { get; set; }
    }

    public class CommitteeDecisionVm
    {
        public Guid IdeaId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int? CombinedAverage { get; set; }
        public bool CanAccept { get; set; }
        public string? ExtraConfirmWarning { get; set; }
        public string? Reason { get; set; }
        public bool ShowRejectBox { get; set; }
    }

    public sealed class CommitteeRejectVm
    {
        public Guid IdeaId { get; set; }

        [Required(ErrorMessage = "سبب الرفض مطلوب.")]
        [MinLength(10, ErrorMessage = "سبب الرفض يجب ألا يقل عن 10 أحرف.")]
        [NoHtml]
        public string? Reason { get; set; }
    }

    public class CommitteeDelegationsVm
    {
        public bool IsHead { get; set; }
        public string? DelegateName { get; set; }
        public DateTime? ActiveFrom { get; set; }
        public DateTime? ActiveTo { get; set; }
        public List<DelegationMemberOptionVm> Candidates { get; set; } = new();
        public List<DelegationRowVm> Rows { get; set; } = new();
        public Guid? DelegateMemberUserId { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
    }

    public sealed class CommitteeDelegationCreateVm
    {
        [Required(ErrorMessage = "يرجى اختيار العضو المفوَّض له.")]
        public Guid? DelegateMemberUserId { get; set; }

        [Required(ErrorMessage = "يرجى تحديد تاريخ بداية التفويض.")]
        public DateTime? StartAt { get; set; }

        [Required(ErrorMessage = "يرجى تحديد تاريخ نهاية التفويض.")]
        public DateTime? EndAt { get; set; }
    }

    public sealed record DelegationMemberOptionVm(Guid UserId, string FullName, string Username);
    public sealed record DelegationRowVm(Guid Id, string DelegateName, DateTime StartAt, DateTime EndAt, bool IsActive);
}
