using Ibtikar.Services.Implementations;

namespace Ibtikar.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, AuditLogService auditLogService)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception at {Path}", context.Request.Path);
                await WriteAuditAsync(auditLogService, ex, context, context.RequestAborted);
                await WriteErrorResponseAsync(context);
            }
        }

        private static async Task WriteAuditAsync(AuditLogService auditLogService, Exception ex, HttpContext context, CancellationToken ct)
        {
            try
            {
                var payload = $"{ex.GetType().FullName}: {ex.Message}";
                await auditLogService.WriteAsync("UnhandledException", "HttpRequest", context.Request.Path, payload, null, ct);
            }
            catch
            {
            }
        }

        private static Task WriteErrorResponseAsync(HttpContext context)
        {
            if (context.Response.HasStarted) return Task.CompletedTask;
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.Redirect("/Error");
            return Task.CompletedTask;
        }
    }
}