using System.Security.Claims;
using Ibtikar.Models;
using Ibtikar.Repositories;

namespace Ibtikar.Services.Implementations
{
    public class AuditLogService
    {
        private readonly IAuditLogRepository _repo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(IAuditLogRepository repo, IHttpContextAccessor httpContextAccessor)
        {
            _repo = repo;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task WriteAsync(string action, string entityName, string? entityId, string? newValues, string? oldValues, CancellationToken ct)
        {
            var (userId, ip, userAgent) = ResolveCallerContext();

            var entry = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ip,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(entry, ct);
            await _repo.SaveChangesAsync(ct);
        }

        private (Guid? userId, string? ip, string? userAgent) ResolveCallerContext()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return (null, null, null);

            var userIdValue = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Guid? userId = Guid.TryParse(userIdValue, out var parsed) ? parsed : null;

            var ip = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            if (userAgent.Length > 512) userAgent = userAgent[..512];

            return (userId, ip, userAgent);
        }
    }
}