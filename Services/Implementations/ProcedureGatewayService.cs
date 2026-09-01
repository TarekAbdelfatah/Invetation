using Microsoft.Extensions.Options;

namespace Ibtikar.Services.Implementations
{
    public sealed class ProcedureGatewayService
    {
        private readonly HttpClient _http;
        private readonly IntegrationOptions _options;
        private readonly ILogger<ProcedureGatewayService> _logger;

        public ProcedureGatewayService(
            HttpClient http,
            IOptions<IntegrationOptions> options,
            ILogger<ProcedureGatewayService> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public async Task NotifyAsync(string referenceNumber, CancellationToken ct = default)
        {
            Validation().Reference(referenceNumber);
            if (string.IsNullOrWhiteSpace(_options.ProceduresEndpoint))
            {
                _logger.LogWarning("ProceduresEndpoint not configured; skipping notify for {Reference}", referenceNumber);
                return;
            }

            await SendWithRetryAsync(referenceNumber, ct);
        }

        private async Task SendWithRetryAsync(string referenceNumber, CancellationToken ct)
        {
            var payload = new { referenceNumber, notifiedAt = DateTime.UtcNow };
            var attempts = Math.Max(0, _options.ProceduresRetryCount) + 1;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, _options.ProceduresEndpoint)
                    {
                        Content = JsonContent.Create(payload)
                    };
                    if (!string.IsNullOrWhiteSpace(_options.ProceduresApiKey))
                    {
                        request.Headers.Add("X-Api-Key", _options.ProceduresApiKey);
                    }

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.ProceduresTimeoutSeconds));

                    using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Procedure notify ok for {Reference} on attempt {Attempt}", referenceNumber, attempt);
                        return;
                    }

                    _logger.LogWarning("Procedure notify failed for {Reference} on attempt {Attempt}: HTTP {Status}",
                        referenceNumber, attempt, (int)response.StatusCode);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning("Procedure notify timed out for {Reference} on attempt {Attempt}",
                        referenceNumber, attempt);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Procedure notify transport error for {Reference} on attempt {Attempt}",
                        referenceNumber, attempt);
                }

                if (attempt < attempts)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), ct);
                }
            }

            _logger.LogError("Procedure notify exhausted retries for {Reference}", referenceNumber);
        }

        private static Validator Validation() => new();

        private sealed class Validator
        {
            public Validator Reference(string reference)
            {
                if (string.IsNullOrWhiteSpace(reference))
                    throw new ArgumentException("Reference is required.", nameof(reference));
                return this;
            }
        }
    }
}
