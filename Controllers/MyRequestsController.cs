using Ibtikar.Data;
using Ibtikar.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.ExternalBeneficiary + "," + RoleCodes.InternalBeneficiary)]
    public class MyRequestsController : Controller
    {
        private readonly IbtikarDbContext _db;
        public MyRequestsController(IbtikarDbContext db) => _db = db;

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdRaw, out var userId)) return Challenge();

            var myIdeas = await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => i.ApplicantUserId == userId)
                .Include(i => i.CurrentStatus)
                .Include(i => i.InnovationDomain)
                .OrderByDescending(i => i.CreatedAt)
                .Take(50)
                .Select(i => new MyRequestVm(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.IsDraft,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#888",
                    i.CreatedAt,
                    i.SubmittedAt))
                .ToListAsync(ct);

            return View(new MyRequestsVm(myIdeas));
        }
    }

    public record MyRequestVm(
        Guid Id,
        string Reference,
        string Title,
        bool IsDraft,
        string StatusName,
        string StatusColor,
        DateTime CreatedAt,
        DateTime? SubmittedAt);

    public class MyRequestsVm
    {
        public List<MyRequestVm> Items { get; }
        public MyRequestsVm(List<MyRequestVm> items) => Items = items;
    }
}
