using Ibtikar.Services.Integrations;
using Microsoft.Extensions.Options;

namespace Ibtikar.Services.Notifications
{
    public sealed class NotificationService : INotificationClient
    {
        private readonly HttpClient _http;
        private readonly IntegrationOptions _options;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            HttpClient http,
            IOptions<IntegrationOptions> options,
            ILogger<NotificationService> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string action, string entityId, IDictionary<string, string>? payload = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_options.NotificationEndpoint))
            {
                _logger.LogWarning("NotificationEndpoint not configured; skipping {Action}", action);
                return false;
            }

            var body = new Dictionary<string, object?>
            {
                ["action"] = action,
                ["entityId"] = entityId,
                ["timestamp"] = DateTime.UtcNow
            };
            if (payload is { Count: > 0 })
            {
                foreach (var kv in payload) body[kv.Key] = kv.Value;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, _options.NotificationEndpoint)
                {
                    Content = JsonContent.Create(body)
                };
                if (!string.IsNullOrWhiteSpace(_options.NotificationApiKey))
                {
                    request.Headers.Add("X-Api-Key", _options.NotificationApiKey);
                }

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.NotificationTimeoutSeconds));

                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
                if (response.IsSuccessStatusCode) return true;

                _logger.LogWarning("Notification {Action} for {Entity} returned {Status}",
                    action, entityId, (int)response.StatusCode);
                return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException || ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Notification {Action} for {Entity} failed", action, entityId);
                return false;
            }
        }
    }
}
