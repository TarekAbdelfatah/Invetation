using Ibtikar.Data;
using Ibtikar.Repositories.Interfaces;
using Ibtikar.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories.Implementations
{
    public sealed class EmployeeRepository : IEmployeeRepository
    {
        private readonly CommonSysDbContext _commonDb;
        private readonly ILogger<EmployeeRepository> _logger;

        public EmployeeRepository(
            CommonSysDbContext commonDb,
            ILogger<EmployeeRepository> logger)
        {
            _commonDb = commonDb;
            _logger = logger;
        }

        public async Task<List<ErpEmployeeOption>> GetEmployeesAsync(CancellationToken ct = default)
        {
            var rawList = await _commonDb.Employees
                .AsNoTracking()
                .Where(e => e.NetworkUser != null && e.NetworkUser != "")
                .Select(e => new { e.NetworkUser, e.Name })
                .ToListAsync(ct);

            return rawList
                .Where(e => !string.IsNullOrWhiteSpace(e.NetworkUser))
                .Select(e => new ErpEmployeeOption(
                    e.NetworkUser,
                    string.IsNullOrWhiteSpace(e.Name) ? e.NetworkUser : e.Name.Trim()))
                .OrderBy(e => e.FullName)
                .ToList();
        }
    }
}
