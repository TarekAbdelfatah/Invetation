namespace Ibtikar.DTOs.Committee
{
    public sealed record CommitteeDashboardDto(
        int UnderStudy,
        int UnderVoting,
        int Accepted,
        int Rejected);

    public sealed record CommitteeReferralRowDto(
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

    public sealed record CommitteeReferralsDto(
        IReadOnlyList<CommitteeReferralRowDto> Items,
        string StatusFilter);
}
