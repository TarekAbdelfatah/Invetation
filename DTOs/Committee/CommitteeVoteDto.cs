namespace Ibtikar.DTOs.Committee
{
    public sealed record CommitteeVoteRowDto(
        Guid IdeaId,
        string Reference,
        string Title,
        string StatusCode,
        string StatusName,
        string StatusColor,
        bool HasVoted,
        string? MyVote);

    public sealed record CommitteeVotesDto(
        IReadOnlyList<CommitteeVoteRowDto> Items);

    public sealed record CommitteeVoteSubmitDto(
        Guid IdeaId,
        string Decision);

    public sealed record CommitteeVoteOutcomeDto(bool Success, string Message);
}
