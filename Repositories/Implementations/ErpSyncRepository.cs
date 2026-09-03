using Ibtikar.Data;
using Ibtikar.Repositories.Interfaces;
using Ibtikar.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories.Implementations
{
    public sealed class ErpSyncRepository : IErpSyncRepository
    {
        private readonly CommonSysDbContext _commonDb;
        private readonly IbtikarDbContext _db;
        private readonly ILogger<ErpSyncRepository> _logger;

        public ErpSyncRepository(
            CommonSysDbContext commonDb,
            IbtikarDbContext db,
            ILogger<ErpSyncRepository> logger)
        {
            _commonDb = commonDb;
            _db = db;
            _logger = logger;
        }

        public async Task<List<ErpEmployeeOption>> GetErpEmployeesAsync(CancellationToken ct = default)
        {
            try
            {
                var erpEmployees = await _commonDb.Employees
                    .AsNoTracking()
                    .Where(e => e.NetworkUser != null && e.NetworkUser != "")
                    .Select(e => new { e.NetworkUser, e.Name })
                    .ToListAsync(ct);

                var result = erpEmployees
                    .Where(e => !string.IsNullOrWhiteSpace(e.NetworkUser))
                    .Select(e => new ErpEmployeeOption(
                        e.NetworkUser,
                        string.IsNullOrWhiteSpace(e.Name) ? e.NetworkUser : e.Name.Trim()))
                    .OrderBy(e => e.FullName)
                    .ToList();

                if (result.Count > 0)
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ErpDatabaseSync Employees table query failed. Falling back to local Users table.");
            }

            // Fallback to local Users
            return await _db.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new ErpEmployeeOption(u.Username, u.FullName))
                .ToListAsync(ct);
        }

        public async Task<List<ErpDepartmentOption>> GetErpHrDepartmentsAsync(CancellationToken ct = default)
        {
            try
            {
                var erpDepts = await _commonDb.HrDepartments
                    .AsNoTracking()
                    .Where(d => d.DeptName != null && d.DeptName != "")
                    .OrderBy(d => d.DeptName)
                    .Select(d => new ErpDepartmentOption((int)d.DeptId, d.DeptName))
                    .ToListAsync(ct);

                if (erpDepts.Count > 0)
                {
                    return erpDepts.OrderBy(d => d.DeptName).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ErpDatabaseSync HR_DEPARTMENT query failed. Falling back to local Departments table.");
            }

            // Fallback to local Departments
            var depts = await _db.Departments
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync(ct);

            var list = new List<ErpDepartmentOption>();
            int idCounter = 1;
            foreach (var d in depts)
            {
                list.Add(new ErpDepartmentOption(idCounter++, d.Name));
            }
            return list;
        }
    }
}
