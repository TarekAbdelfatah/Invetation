using Ibtikar.Data;
using Ibtikar.DTOs.Account;
using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly IbtikarDbContext _db;

        public UserRepository(IbtikarDbContext db) => _db = db;

        public async Task<User?> GetActiveByUsernameWithRolesAsync(string username, CancellationToken ct)
            => await _db.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct);

        public async Task<bool> ExistsAsync(Guid id, CancellationToken ct)
            => await _db.Users.AsNoTracking().AnyAsync(u => u.Id == id, ct);

        public async Task<bool> ExistsAndActiveAsync(Guid id, CancellationToken ct)
            => await _db.Users.AsNoTracking().AnyAsync(u => u.Id == id && u.IsActive, ct);

        public async Task<IReadOnlyList<Guid>> GetActiveUserIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
            => await _db.Users
                .AsNoTracking()
                .Where(u => ids.Contains(u.Id) && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(ct);

        public async Task<IReadOnlyList<DemoUserDto>> GetDemoUsersAsync(CancellationToken ct)
            => await _db.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Username)
                .Select(u => new DemoUserDto(u.Username, u.FullName, "User"))
                .ToListAsync(ct);

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}