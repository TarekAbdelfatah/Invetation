using Ibtikar.Data;
using Ibtikar.Repositories.Interfaces;
using Ibtikar.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories.Implementations
{
    public sealed class DepartmentRepository : IDepartmentRepository
    {
        private readonly CommonSysDbContext _commonDb;
        private readonly ILogger<DepartmentRepository> _logger;

        public DepartmentRepository(
            CommonSysDbContext commonDb,
            ILogger<DepartmentRepository> logger)
        {
            _commonDb = commonDb;
            _logger = logger;
        }

        public async Task<List<ErpDepartmentOption>> GetHrDepartmentsAsync(CancellationToken ct = default)
        {
            return await _commonDb.HrDepartments
                .AsNoTracking()
                .Where(d => d.DeptName != null && d.DeptName != "")
                .OrderBy(d => d.DeptName)
                .Select(d => new ErpDepartmentOption((int)d.DeptId, d.DeptName))
                .ToListAsync(ct);
        }
    }
}
