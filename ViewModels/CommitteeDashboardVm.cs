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
}
