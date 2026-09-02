using Ibtikar.Data;
using Ibtikar.Repositories.Interfaces;
using Ibtikar.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Repositories.Implementations
{
    public sealed class ErpSyncRepository : IErpSyncRepository
    {
        private readonly IbtikarDbContext _db;
        private readonly ILogger<ErpSyncRepository> _logger;

        public ErpSyncRepository(IbtikarDbContext db, ILogger<ErpSyncRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<ErpEmployeeOption>> GetErpEmployeesAsync(CancellationToken ct = default)
        {
            try
            {
                if (_db.Database.IsSqlServer())
                {
                    var sql = @"
                        IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'ErpDatabaseSync')
                        AND EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[ErpDatabaseSync].[dbo].[Employees]') AND type in (N'U', N'V'))
                        BEGIN
                            SELECT DISTINCT 
                                CAST([NetworkUser] AS nvarchar(150)) AS NetworkUser,
                                CAST(COALESCE([Name], [NetworkUser]) AS nvarchar(200)) AS FullName
                            FROM [ErpDatabaseSync].[dbo].[Employees]
                            WHERE [NetworkUser] IS NOT NULL AND LTRIM(RTRIM([NetworkUser])) <> ''
                        END";

                    var erpEmployees = await _db.Database.SqlQueryRaw<ErpEmployeeOption>(sql).ToListAsync(ct);
                    if (erpEmployees.Count > 0)
                    {
                        return erpEmployees
                            .Where(e => !string.IsNullOrWhiteSpace(e.NetworkUser))
                            .OrderBy(e => e.FullName)
                            .ToList();
                    }
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
                if (_db.Database.IsSqlServer())
                {
                    var sql = @"
                        IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'ErpDatabaseSync')
                        AND EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[ErpDatabaseSync].[COMMON_SYS].[HR_DEPARTMENT]') AND type in (N'U', N'V'))
                        BEGIN
                            SELECT CAST([DeptId] AS int) AS DeptId, CAST([DeptName] AS nvarchar(200)) AS DeptName
                            FROM [ErpDatabaseSync].[COMMON_SYS].[HR_DEPARTMENT]
                            WHERE [DeptName] IS NOT NULL AND LTRIM(RTRIM([DeptName])) <> ''
                        END";

                    var erpDepts = await _db.Database.SqlQueryRaw<ErpDepartmentOption>(sql).ToListAsync(ct);
                    if (erpDepts.Count > 0)
                    {
                        return erpDepts.OrderBy(d => d.DeptName).ToList();
                    }
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
