namespace Ibtikar.DTOs.Committee
{
    public sealed record CommitteeDashboardDto(
        int UnderStudy,
        int UnderVoting,
        int Accepted,
        int Rejected);

    public sealed record CommitteeReferralListDto(
        IReadOnlyList<CommitteeReferralRowDto> Items,
        int Page,
        int PageSize,
        int TotalCount);

    public sealed record CommitteeReferralRowDto(
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
        bool HasAddedCommitteeAssessment = false,
        bool HasVoted = false,
        string? DecisionNote = null);
}
