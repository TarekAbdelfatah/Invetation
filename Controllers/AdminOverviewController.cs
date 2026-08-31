using Ibtikar.DTOs.AdminOverview;
using Ibtikar.Services.Admin;
using Ibtikar.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.SystemAdmin)]
    public class AdminOverviewController : Controller
    {
        private readonly IAdminOverviewService _service;

        public AdminOverviewController(IAdminOverviewService service) => _service = service;

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var dto = await _service.GetSnapshotAsync(ct);
            return View(ToVm(dto));
        }

        private static AdminOverviewVm ToVm(AdminOverviewDto dto)
            => new()
            {
                TotalIdeas = dto.TotalIdeas,
                Drafts = dto.Drafts,
                Submitted = dto.Submitted,
                TotalUsers = dto.TotalUsers,
                ByStatus = dto.ByStatus
                    .Select(s => new AdminOverviewVm.StatusCount(s.Code, s.Name, s.Color, s.Count))
                    .ToList(),
                Recent = dto.Recent
                    .Select(r => new AdminOverviewVm.RecentIdea(
                        r.Reference,
                        r.Title,
                        r.StatusName,
                        r.StatusColor,
                        r.Domain,
                        r.CreatedAt))
                    .ToList()
            };
    }

    public class AdminOverviewVm
    {
        public int TotalIdeas { get; set; }
        public int Drafts { get; set; }
        public int Submitted { get; set; }
        public int TotalUsers { get; set; }
        public List<StatusCount> ByStatus { get; set; } = new();
        public List<RecentIdea> Recent { get; set; } = new();

        public record StatusCount(string Code, string Name, string Color, int Count);
        public record RecentIdea(string Reference, string Title, string StatusName, string StatusColor, string Domain, DateTime CreatedAt);
    }
}