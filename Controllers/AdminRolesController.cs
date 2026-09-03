using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Repositories.Interfaces;
using Ibtikar.Services.Helpers;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Controllers
{
    /// <summary>
    /// Administrative Role Assignment Controller.
    /// Enables System Administrators to assign roles to users from CommonSys EmployeeRepository and DepartmentRepository,
    /// select specialized department (if applicable), and persist into Admins table.
    /// </summary>
    [IbtikarAuthorize(RoleCodes.SystemAdmin)]
    public class AdminRolesController : Controller
    {
        private readonly IbtikarDbContext _db;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IDepartmentRepository _deptRepo;
        private readonly ILogger<AdminRolesController> _logger;

        public AdminRolesController(
            IbtikarDbContext db,
            IEmployeeRepository employeeRepo,
            IDepartmentRepository deptRepo,
            ILogger<AdminRolesController> logger)
        {
            _db = db;
            _employeeRepo = employeeRepo;
            _deptRepo = deptRepo;
            _logger = logger;
        }

        [HttpGet("/AdminRoles")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var vm = await BuildIndexVmAsync(new AssignAdminRoleInputVm(), ct);
            return View(vm);
        }

        [HttpPost("/AdminRoles/Assign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignAdminRoleInputVm input, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(input.NetworkUser))
            {
                ModelState.AddModelError(nameof(input.NetworkUser), "يرجى اختيار الموظف.");
            }

            if (input.RoleId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(input.RoleId), "يرجى اختيار الصلاحية.");
            }

            var selectedRole = await _db.Roles.FirstOrDefaultAsync(r => r.Id == input.RoleId, ct);
            if (selectedRole is null)
            {
                ModelState.AddModelError(nameof(input.RoleId), "الصلاحية المختارة غير موجودة.");
            }
            else if (string.Equals(selectedRole.Code, RoleCodes.SpecializedDepartment, StringComparison.OrdinalIgnoreCase))
            {
                if (!input.DeptId.HasValue || input.DeptId.Value <= 0)
                {
                    ModelState.AddModelError(nameof(input.DeptId), "يرجى اختيار الإدارة المختصة.");
                }
            }

            if (!ModelState.IsValid)
            {
                var vm = await BuildIndexVmAsync(input, ct);
                return View(nameof(Index), vm);
            }

            // Normalize network username (strip @bog.gov.sa if present)
            var networkUser = NormalizeUsername(input.NetworkUser);

            var existingAdmin = await _db.Admins.FirstOrDefaultAsync(a => a.NetworkUser == networkUser, ct);
            if (existingAdmin is null)
            {
                _db.Admins.Add(new Admin
                {
                    NetworkUser = networkUser,
                    RoleId = input.RoleId,
                    DeptId = input.DeptId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
                TempData["SuccessMessage"] = $"تم إسناد صلاحية ({selectedRole!.Name}) للموظف ({networkUser}) بنجاح.";
            }
            else
            {
                existingAdmin.RoleId = input.RoleId;
                existingAdmin.DeptId = input.DeptId;
                existingAdmin.IsActive = true;
                TempData["SuccessMessage"] = $"تم تحديث صلاحية الموظف ({networkUser}) إلى ({selectedRole!.Name}) بنجاح.";
            }

            await _db.SaveChangesAsync(ct);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/AdminRoles/ToggleStatus/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, CancellationToken ct)
        {
            var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (admin is not null)
            {
                admin.IsActive = !admin.IsActive;
                await _db.SaveChangesAsync(ct);
                TempData["SuccessMessage"] = admin.IsActive ? "تم تفعيل حساب الصلاحية بنجاح." : "تم تعطيل حساب الصلاحية بنجاح.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<AdminRoleIndexVm> BuildIndexVmAsync(AssignAdminRoleInputVm input, CancellationToken ct)
        {
            var employees = await _employeeRepo.GetEmployeesAsync(ct);
            var hrDepartments = await _deptRepo.GetHrDepartmentsAsync(ct);
            var systemRoles = await _db.Roles
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .Select(r => new RoleOptionDto(r.Id, r.Code, r.Name))
                .ToListAsync(ct);

            var rawAdmins = await _db.Admins
                .Include(a => a.Role)
                .AsNoTracking()
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);

            var localUsersMap = await _db.Users
                .AsNoTracking()
                .Select(u => new { u.Username, u.FullName })
                .ToListAsync(ct);

            var empMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var emp in employees)
            {
                var key = NormalizeUsername(emp.NetworkUser);
                if (!string.IsNullOrWhiteSpace(key) && !empMap.ContainsKey(key))
                {
                    empMap[key] = emp.FullName;
                }
            }

            var dbUserMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var u in localUsersMap)
            {
                var key = NormalizeUsername(u.Username);
                if (!string.IsNullOrWhiteSpace(key) && !dbUserMap.ContainsKey(key))
                {
                    dbUserMap[key] = u.FullName;
                }
            }

            var deptMap = hrDepartments.ToDictionary(d => d.DeptId, d => d.DeptName);

            var rows = new List<AdminRoleRowDto>();
            foreach (var a in rawAdmins)
            {
                var normUser = NormalizeUsername(a.NetworkUser);
                var name = empMap.TryGetValue(normUser, out var n) && !string.IsNullOrWhiteSpace(n)
                    ? n
                    : (dbUserMap.TryGetValue(normUser, out var dbName) && !string.IsNullOrWhiteSpace(dbName) ? dbName : normUser);

                var deptName = a.DeptId.HasValue && deptMap.TryGetValue(a.DeptId.Value, out var dn) ? dn : null;

                rows.Add(new AdminRoleRowDto(
                    a.Id,
                    normUser,
                    name,
                    a.Role?.Code ?? string.Empty,
                    a.Role?.Name ?? "غير مخصص",
                    a.DeptId,
                    deptName,
                    a.IsActive,
                    a.CreatedAt
                ));
            }

            return new AdminRoleIndexVm
            {
                Employees = employees,
                Departments = hrDepartments,
                Roles = systemRoles,
                AssignedAdmins = rows,
                Input = input
            };
        }

        private static string NormalizeUsername(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var u = raw.Trim();
            if (u.EndsWith("@bog.gov.sa", StringComparison.OrdinalIgnoreCase))
            {
                u = u.Substring(0, u.Length - "@bog.gov.sa".Length);
            }
            return u;
        }
    }
}
