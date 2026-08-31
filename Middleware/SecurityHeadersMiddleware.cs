using Microsoft.AspNetCore.Http;

namespace Ibtikar.Middleware
{
    public sealed class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                if (!headers.ContainsKey("Content-Security-Policy"))
                {
                    headers["Content-Security-Policy"] =
                        "default-src 'self'; " +
                        "img-src 'self' data:; " +
                        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                        "font-src 'self' https://fonts.gstatic.com; " +
                        "script-src 'self' https://cdn.jsdelivr.net 'unsafe-inline'; " +
                        "connect-src 'self'; " +
                        "frame-ancestors 'none';";
                }
                return Task.CompletedTask;
            });
            return _next(context);
        }
    }
}
