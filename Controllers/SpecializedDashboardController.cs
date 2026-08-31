using Ibtikar.DTOs.SpecializedDashboard;
using Ibtikar.Services.Security;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.SpecializedDepartment)]
    public class SpecializedDashboardController : Controller
    {
        private readonly Services.SpecializedDashboard.ISpecializedDashboardService _service;

        public SpecializedDashboardController(Services.SpecializedDashboard.ISpecializedDashboardService service)
            => _service = service;

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var departmentId = ResolveDepartmentId();
            var dto = await _service.GetSnapshotAsync(departmentId, ct);

            var vm = new SpecializedDashboardVm
            {
                UnderStudy = dto?.UnderStudy ?? 0,
                SentToPartner = dto?.SentToPartner ?? 0,
                SentToExecution = dto?.SentToExecution ?? 0,
                RejectedAfterRouting = dto?.RejectedAfterRouting ?? 0,
                DepartmentName = User.FindFirst("ibtikar_department_name")?.Value
            };
            return View(vm);
        }

        private Guid? ResolveDepartmentId()
        {
            var raw = User.FindFirst(RoleCodes.DepartmentIdClaim)?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }
}