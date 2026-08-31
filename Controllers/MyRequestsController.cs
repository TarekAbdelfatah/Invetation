using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services.Attachments;
using Ibtikar.Services.Ideas;
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
        private readonly IdeaOwnerQuery _ownerQuery;
        private readonly FileStorageService _storage;

        public MyRequestsController(IbtikarDbContext db, IdeaOwnerQuery ownerQuery, FileStorageService storage)
        {
            _db = db;
            _ownerQuery = ownerQuery;
            _storage = storage;
        }

        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var applicantId = ResolveApplicantId();
            if (applicantId == Guid.Empty) return Challenge();

            var myIdeas = await _ownerQuery
                .ForCurrentApplicant(_db.InnovationIdeas, applicantId)
                .AsNoTracking()
                .Include(i => i.CurrentStatus)
                .Include(i => i.InnovationDomain)
                .OrderByDescending(i => i.CreatedAt)
                .Take(50)
                .Select(i => new
                {
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.IsDraft,
                    StatusName = i.IsDraft
                        ? "مسودة"
                        : (i.CurrentStatus != null ? i.CurrentStatus.Name : "—"),
                    StatusColor = i.IsDraft
                        ? "#6c757d"
                        : (i.CurrentStatus != null ? i.CurrentStatus.Color : "#888"),
                    i.CreatedAt,
                    i.SubmittedAt
                })
                .ToListAsync(ct);

            var items = myIdeas.Select(i => new MyRequestVm(
                i.Id,
                i.ReferenceNumber,
                i.Title,
                i.IsDraft,
                i.StatusName,
                i.StatusColor,
                i.CreatedAt,
                i.SubmittedAt))
                .ToList();

            return View(new MyRequestsVm(items));
        }

        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var applicantId = ResolveApplicantId();
            if (applicantId == Guid.Empty) return Challenge();

            var idea = await _ownerQuery
                .ForCurrentApplicant(_db.InnovationIdeas, applicantId)
                .AsNoTracking()
                .Include(i => i.CurrentStatus)
                .Include(i => i.InnovationDomain)
                .Include(i => i.ExpectedImpact)
                .Include(i => i.TargetAudience)
                .Include(i => i.Attachments)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (idea is null) return NotFound();

            return View(MyRequestDetailsVm.FromEntity(idea));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var applicantId = ResolveApplicantId();
            if (applicantId == Guid.Empty) return Challenge();

            var idea = await _ownerQuery
                .ForCurrentApplicant(_db.InnovationIdeas, applicantId)
                .Include(i => i.CurrentStatus)
                .Include(i => i.Attachments)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (idea is null) return NotFound();

            if (!IsDeletableNewIdea(idea))
                return BadRequest("لا يمكن حذف الطلب بعد أن يبدأ الفريق المختص دراسته.");

            foreach (var attachment in idea.Attachments)
            {
                _storage.Delete(attachment.StoragePath);
            }

            _db.IdeaAttachments.RemoveRange(idea.Attachments);
            _db.InnovationIdeas.Remove(idea);
            await _db.SaveChangesAsync(ct);

            TempData["IdeaDeleted"] = "تم حذف الطلب.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(Guid attachmentId, CancellationToken ct)
        {
            var applicantId = ResolveApplicantId();
            if (applicantId == Guid.Empty) return Challenge();

            var attachment = await _db.IdeaAttachments
                .AsNoTracking()
                .Include(a => a.InnovationIdea)
                .FirstOrDefaultAsync(a => a.Id == attachmentId, ct);

            if (attachment is null) return NotFound();
            if (attachment.InnovationIdea.ApplicantUserId != applicantId) return Forbid();

            return UnprocessableEntity(new
            {
                error = "يرجى رفع الملفات الأساسية لإتمام العملية. المرفقات إلزامية ولا يمكن حذفها، تواصل مع مدير النظام للاستبدال."
            });
        }

        private static bool IsDeletableNewIdea(InnovationIdea idea) =>
            !idea.IsDraft && string.Equals(idea.CurrentStatus?.Code, "new", StringComparison.OrdinalIgnoreCase);

        private Guid ResolveApplicantId()
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
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

    public record MyRequestDetailsVm(
        Guid Id,
        string Reference,
        string Title,
        string Description,
        string? ProblemStatement,
        string? ProposedSolution,
        string? ExpectedBenefits,
        string? ExpectedImpactOther,
        string? TargetAudienceOther,
        bool UsesEmergingTech,
        string? TechnologyOther,
        string StatusCode,
        string StatusName,
        string StatusColor,
        string? DomainName,
        string? ExpectedImpactName,
        string? TargetAudienceName,
        DateTime CreatedAt,
        DateTime? SubmittedAt,
        List<MyRequestAttachmentVm> Attachments)
    {
        public static MyRequestDetailsVm FromEntity(InnovationIdea i) => new(
            i.Id,
            i.ReferenceNumber,
            i.Title,
            i.Description,
            i.ProblemStatement,
            i.ProposedSolution,
            i.ExpectedBenefits,
            i.ExpectedImpactOther,
            i.TargetAudienceOther,
            i.UsesEmergingTech,
            i.TechnologyOther,
            i.CurrentStatus?.Code ?? string.Empty,
            i.CurrentStatus?.Name ?? "—",
            i.CurrentStatus?.Color ?? "#6c757d",
            i.InnovationDomain?.Name,
            i.ExpectedImpact?.Name,
            i.TargetAudience?.Name,
            i.CreatedAt,
            i.SubmittedAt,
            (i.Attachments ?? new List<IdeaAttachment>())
                .OrderBy(a => a.UploadedAt)
                .Select(a => new MyRequestAttachmentVm(a.Id, a.FileName, a.SizeBytes, a.UploadedAt))
                .ToList());
    }

    public record MyRequestAttachmentVm(Guid Id, string FileName, long SizeBytes, DateTime UploadedAt);
}