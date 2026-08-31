using Ibtikar.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.PartnerDepartment)]
    public class PartnerDashboardController : Controller
    {
        public IActionResult Index() => View();
    }

    [Authorize(Roles = RoleCodes.InnovationCommitteeMember)]
    public class CommitteeController : Controller
    {
        public IActionResult Index() => View();
    }
}