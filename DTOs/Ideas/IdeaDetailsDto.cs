namespace Ibtikar.DTOs.Ideas
{
    public sealed record IdeaDetailsDto(
        string ReferenceNumber,
        string Title,
        string StatusName,
        string StatusColor,
        string DomainName,
        DateTime SubmittedAt);
}