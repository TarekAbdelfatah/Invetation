using Ibtikar.Data;
using Ibtikar.Models;

namespace Ibtikar.Repositories
{
    public sealed class AuditLogRepository : IAuditLogRepository
    {
        private readonly IbtikarDbContext _db;

        public AuditLogRepository(IbtikarDbContext db) => _db = db;

        public async Task AddAsync(AuditLog entry, CancellationToken ct)
            => await _db.AuditLogs.AddAsync(entry, ct);

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}