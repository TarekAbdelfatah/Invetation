using Ibtikar.DTOs.Notifications;

namespace Ibtikar.Services.Interfaces
{
    public interface IExternalNotificationService
    {
        Task<ExternalNotificationResult> SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default);
        Task<ExternalNotificationResult> SendEmailAsync(string toIdentity, string subject, string body, CancellationToken ct = default);
    }

    public sealed record ExternalNotificationResult(
        bool Success,
        string? TraceId,
        IReadOnlyList<ExternalNotificationError>? Errors = null)
    {
        public static ExternalNotificationResult Ok(string? traceId) =>
            new(true, traceId);

        public static ExternalNotificationResult Failed(string? traceId, IReadOnlyList<ExternalNotificationError>? errors) =>
            new(false, traceId, errors);

        public static ExternalNotificationResult Skipped() =>
            new(false, null);
    }
}
