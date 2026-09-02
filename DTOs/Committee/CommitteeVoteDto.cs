namespace Ibtikar.DTOs.Committee
{
    public sealed record CommitteeVoteIdeaDto(
        Guid IdeaId,
        string Reference,
        string Title,
        string StatusCode,
        string StatusName,
        string StatusColor,
        string Description,
        string? ProblemStatement,
        string? ProposedSolution,
        string? ExpectedBenefits,
        CommitteeIdeaReadOnlyDto Idea);

    public sealed record CommitteeVoteRowDto(
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
        CommitteeIdeaReadOnlyDto Idea);

    public sealed record CommitteeVotesDto(
        IReadOnlyList<CommitteeVoteRowDto> Items);

    public sealed record CommitteeVoteSubmitDto(
        Guid IdeaId,
        string Decision);

    public sealed record CommitteeVoteOutcomeDto(bool Success, string Message);
}
