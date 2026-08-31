using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services.Ideas;
using Ibtikar.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.AuditEmployee)]
    public class AuditController : Controller
    {
        private readonly IbtikarDbContext _db;
        public AuditController(IbtikarDbContext db) => _db = db;

        public async Task<IActionResult> Inbox(CancellationToken ct)
        {
            var inbox = await _db.IdeaStatuses
                .Where(s => s.Code == IdeaStatusCodes.New || s.Code == IdeaStatusCodes.Resubmitted)
                .Select(s => s.Id)
                .ToListAsync(ct);

            var items = await _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => inbox.Contains(i.CurrentStatusId))
                .Include(i => i.CurrentStatus)
                .Include(i => i.InnovationDomain)
                .Include(i => i.ApplicantDepartment)
                .OrderByDescending(i => i.CreatedAt)
                .Take(50)
                .Select(i => new AuditInboxVm.Row(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.InnovationDomain != null ? i.InnovationDomain.Name : "—",
                    i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : "—",
                    i.CreatedAt))
                .ToListAsync(ct);

            return View(new AuditInboxVm(items));
        }
    }

    public class AuditInboxVm
    {
        public List<Row> Items { get; }
        public AuditInboxVm(List<Row> items) => Items = items;
        public record Row(Guid Id, string Reference, string Title, string Domain, string Department, DateTime SubmittedAt);
    }
}
