using System.Text.Json.Serialization;

namespace Ibtikar.DTOs.Notifications
{
    public sealed record SmsNotificationDto(
        [property: JsonPropertyName("ToIdentity")] string ToIdentity,
        [property: JsonPropertyName("Body")] string Body,
        [property: JsonPropertyName("SystemEmailRefId")] string SystemEmailRefId);

    public sealed record EmailNotificationDto(
        [property: JsonPropertyName("ToIdentity")] string ToIdentity,
        [property: JsonPropertyName("Subject")] string Subject,
        [property: JsonPropertyName("Body")] string Body,
        [property: JsonPropertyName("SystemEmailRefId")] string SystemEmailRefId,
        [property: JsonPropertyName("Priority")] int Priority = 2,
        [property: JsonPropertyName("CC_Mails")] string? CC_Mails = null);

    public sealed record ExternalNotificationResponse(
        [property: JsonPropertyName("data")] object? Data,
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("partialSuccess")] bool PartialSuccess,
        [property: JsonPropertyName("validationResponse")] ExternalNotificationValidation? ValidationResponse,
        [property: JsonPropertyName("traceId")] string? TraceId);

    public sealed record ExternalNotificationValidation(
        [property: JsonPropertyName("status")] bool Status,
        [property: JsonPropertyName("lstErrors")] IReadOnlyList<ExternalNotificationError>? LstErrors,
        [property: JsonPropertyName("timestamp")] DateTime? Timestamp);

    public sealed record ExternalNotificationError(
        [property: JsonPropertyName("refId")] string? RefId,
        [property: JsonPropertyName("errorCode")] string? ErrorCode,
        [property: JsonPropertyName("errorMessage")] string? ErrorMessage);
}
