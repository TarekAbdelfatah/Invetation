using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.SystemAdmin)]
    public class AdminOverviewController : Controller
    {
        private readonly IbtikarDbContext _db;

        public AdminOverviewController(IbtikarDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var model = new AdminOverviewVm
            {
                TotalIdeas = await _db.InnovationIdeas.CountAsync(ct),
                Drafts = await _db.InnovationIdeas.CountAsync(i => i.IsDraft, ct),
                Submitted = await _db.InnovationIdeas.CountAsync(i => !i.IsDraft, ct),
                TotalUsers = await _db.Users.CountAsync(u => u.IsActive, ct),
                ByStatus = await _db.IdeaStatuses
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new AdminOverviewVm.StatusCount(s.Code, s.Name, s.Color,
                        _db.InnovationIdeas.Count(i => i.CurrentStatusId == s.Id)))
                    .ToListAsync(ct),
                Recent = await _db.InnovationIdeas
                    .AsNoTracking()
                    .Include(i => i.CurrentStatus)
                    .Include(i => i.InnovationDomain)
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(8)
                    .Select(i => new AdminOverviewVm.RecentIdea(
                        i.ReferenceNumber,
                        i.Title,
                        i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                        i.CurrentStatus != null ? i.CurrentStatus.Color : "#888",
                        i.InnovationDomain != null ? i.InnovationDomain.Name : "—",
                        i.CreatedAt))
                    .ToListAsync(ct)
            };
            return View(model);
        }
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
