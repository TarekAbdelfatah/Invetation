using Ibtikar.DTOs.AdminOverview;
using Ibtikar.Services.Admin;
using Ibtikar.Services.Security;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.SystemAdmin)]
    public class AdminOverviewController : Controller
    {
        private readonly IAdminOverviewService _service;

        public AdminOverviewController(IAdminOverviewService service) => _service = service;

        public async Task<IActionResult> Index(string? status, CancellationToken ct)
        {
            var snapshot = await _service.GetSnapshotAsync(ct);
            var ideas = await _service.GetIdeasAsync(status, 200, ct);

            return View(ToVm(snapshot, ideas));
        }

        private static AdminOverviewVm ToVm(AdminOverviewDto dto, AdminOverviewListDto ideas)
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
                    .ToList(),
                StatusFilter = ideas.StatusFilter,
                Ideas = ideas.Rows.Select(i => new AdminOverviewVm.IdeaRow(
                    i.Id, i.Reference, i.Title, i.DomainName, i.ApplicantName,
                    i.ApplicantDepartmentName, i.AssignedDepartmentName,
                    i.StatusCode, i.StatusName, i.StatusColor, i.CreatedAt, i.IsDraft)).ToList(),
                IdeasTotalCount = ideas.TotalCount
            };
    }
}