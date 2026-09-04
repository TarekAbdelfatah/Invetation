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

    public sealed record CommitteeDetailDto(
        Guid Id,
        string Name,
        string? Description,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? ActivatedAt,
        Guid HeadUserId,
        string HeadUserName,
        string HeadUsername,
        IReadOnlyList<CommitteeMemberDetailDto> Members);

    public sealed record CommitteeMemberDetailDto(
        Guid UserId,
        string FullName,
        string Username,
        bool IsHead);

    public sealed record CommitteeEditDto(
        Guid CommitteeId,
        string Name,
        string? Description,
        Guid HeadUserId,
        IReadOnlyList<Guid> MemberUserIds);

    public sealed record CommitteeEditResultDto(bool Success, string Message);
}
