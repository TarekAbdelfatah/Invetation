using Ibtikar.DTOs.Account;
using Ibtikar.Models;

namespace Ibtikar.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetActiveByUsernameWithRolesAsync(string username, CancellationToken ct);
        Task<bool> ExistsAsync(Guid id, CancellationToken ct);
        Task<bool> ExistsAndActiveAsync(Guid id, CancellationToken ct);
        Task<IReadOnlyList<Guid>> GetActiveUserIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
        Task<IReadOnlyList<DemoUserDto>> GetDemoUsersAsync(CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}