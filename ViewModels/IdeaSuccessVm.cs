namespace Ibtikar.ViewModels
{
    public record IdeaSuccessVm(
        string ReferenceNumber,
        string Title,
        string StatusName,
        string StatusColor,
        string DomainName,
        DateTime SubmittedAt);
}
