using Ibtikar.Models;

namespace Ibtikar.Repositories
{
    public interface IAuditLogRepository
    {
        Task AddAsync(AuditLog entry, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}