namespace Ibtikar.DTOs.Committees
{
    public sealed record CommitteeSummaryDto(
        Guid Id,
        string Name,
        string? Description,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? ActivatedAt,
        string HeadUserName,
        int MemberCount);

    public sealed record CommitteeMemberOptionDto(Guid UserId, string FullName, string Username, bool IsCurrentHead);

    public sealed record CommitteeCreateDto(
        string Name,
        string? Description,
        Guid HeadUserId,
        IReadOnlyList<Guid> MemberUserIds);

    public sealed record CommitteeCreateResultDto(bool Success, string Message, Guid? CommitteeId);
}
