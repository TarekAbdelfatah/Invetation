namespace Ibtikar.DTOs.SpecializedDashboard
{
    public sealed record SpecializedDashboardDto(
        int UnderStudy,
        int SentToPartner,
        int SentToExecution,
        int RejectedAfterRouting);
}