using System.Net.Http.Json;
using System.Text.Json;
using Ibtikar.DTOs.Notifications;
using Ibtikar.Options;
using Ibtikar.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace Ibtikar.Services.Implementations
{
    public sealed class ExternalNotificationService : IExternalNotificationService
    {
        private readonly HttpClient _http;
        private readonly ExternalNotificationOptions _options;
        private readonly ILogger<ExternalNotificationService> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public ExternalNotificationService(
            HttpClient http,
            IOptions<ExternalNotificationOptions> options,
            ILogger<ExternalNotificationService> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ExternalNotificationResult> SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default)
        {
            if (!_options.SmsEnabled)
            {
                _logger.LogDebug("SMS disabled; skipping to {Phone}", phoneNumber);
                return ExternalNotificationResult.Skipped();
            }

            if (string.IsNullOrWhiteSpace(_options.SmsEndpoint))
            {
                _logger.LogWarning("SmsEndpoint not configured; skipping SMS to {Phone}", phoneNumber);
                return ExternalNotificationResult.Skipped();
            }

            if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning("SMS skipped: phone or message is empty");
                return ExternalNotificationResult.Skipped();
            }

            var dto = new SmsNotificationDto(
                ToIdentity: phoneNumber,
                Body: message,
                SystemEmailRefId: Guid.NewGuid().ToString("N"));

            return await PostAsync(_options.SmsEndpoint, dto, ct);
        }

        public async Task<ExternalNotificationResult> SendEmailAsync(string toIdentity, string subject, string body, CancellationToken ct = default)
        {
            if (!_options.EmailEnabled)
            {
                _logger.LogDebug("Email disabled; skipping to {To}", toIdentity);
                return ExternalNotificationResult.Skipped();
            }

            if (string.IsNullOrWhiteSpace(_options.EmailEndpoint))
            {
                _logger.LogWarning("EmailEndpoint not configured; skipping email to {To}", toIdentity);
                return ExternalNotificationResult.Skipped();
            }

            if (string.IsNullOrWhiteSpace(toIdentity) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            {
                _logger.LogWarning("Email skipped: to, subject, or body is empty");
                return ExternalNotificationResult.Skipped();
            }

            var dto = new EmailNotificationDto(
                ToIdentity: toIdentity,
                Subject: subject,
                Body: body,
                SystemEmailRefId: Guid.NewGuid().ToString("N"));

            return await PostAsync(_options.EmailEndpoint, dto, ct);
        }

        private async Task<ExternalNotificationResult> PostAsync(string endpoint, object payload, CancellationToken ct)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = JsonContent.Create(payload, options: JsonOpts)
                };
                if (!string.IsNullOrWhiteSpace(_options.AppKey))
                    request.Headers.Add("App-Key", _options.AppKey);
                if (!string.IsNullOrWhiteSpace(_options.GatewayApiKey))
                    request.Headers.Add("x-Gateway-APIKey", _options.GatewayApiKey);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("External notification returned HTTP {Status}: {Body}",
                        (int)response.StatusCode, responseBody);
                    return ExternalNotificationResult.Failed(null, null);
                }

                var apiResponse = JsonSerializer.Deserialize<ExternalNotificationResponse>(responseBody, JsonOpts);

                if (apiResponse is null)
                {
                    _logger.LogWarning("External notification returned unparseable body: {Body}", responseBody);
                    return ExternalNotificationResult.Failed(null, null);
                }

                if (apiResponse.Success)
                {
                    _logger.LogInformation("External notification sent successfully. TraceId: {TraceId}", apiResponse.TraceId);
                    return ExternalNotificationResult.Ok(apiResponse.TraceId);
                }

                _logger.LogWarning("External notification failed. TraceId: {TraceId}, Errors: {Errors}",
                    apiResponse.TraceId,
                    apiResponse.ValidationResponse?.LstErrors is { Count: > 0 }
                        ? string.Join("; ", apiResponse.ValidationResponse.LstErrors.Select(e => $"[{e.ErrorCode}] {e.ErrorMessage}"))
                        : "none");

                return ExternalNotificationResult.Failed(
                    apiResponse.TraceId,
                    apiResponse.ValidationResponse?.LstErrors);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("External notification timed out");
                return ExternalNotificationResult.Failed(null, null);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "External notification transport error");
                return ExternalNotificationResult.Failed(null, null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "External notification unexpected error");
                return ExternalNotificationResult.Failed(null, null);
            }
        }
    }
}
