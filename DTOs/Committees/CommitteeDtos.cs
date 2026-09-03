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

    public sealed record CommitteeMemberOptionDto(int AdminId, string FullName, string Username, bool IsCurrentHead);

    public sealed record CommitteeCreateDto(
        string Name,
        string? Description,
        int HeadAdminId,
        IReadOnlyList<int> MemberAdminIds);

    public sealed record CommitteeCreateResultDto(bool Success, string Message, Guid? CommitteeId);
}
