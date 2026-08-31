using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services.Attachments;
using Ibtikar.Services.Ideas;
using Ibtikar.Services.Integrations;
using Ibtikar.Services.Security;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    public class IdeasController : Controller
    {
        private readonly IbtikarDbContext _db;
        private readonly ILogger<IdeasController> _logger;
        private readonly ProcedureGatewayService _procedureGateway;
        private readonly AttachmentService _attachments;

        public IdeasController(
            IbtikarDbContext db,
            ILogger<IdeasController> logger,
            ProcedureGatewayService procedureGateway,
            AttachmentService attachments)
        {
            _db = db;
            _logger = logger;
            _procedureGateway = procedureGateway;
            _attachments = attachments;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                var ideas = await _db.InnovationIdeas
                    .AsNoTracking()
                    .Include(i => i.CurrentStatus)
                    .Include(i => i.InnovationDomain)
                    .Include(i => i.ApplicantDepartment)
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                return View(ideas);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ideas index fallback (database unavailable): {Message}", ex.Message);
                ViewBag.DatabaseError = ex.Message;
                return View(Array.Empty<InnovationIdea>());
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new IdeaCreateViewModel();

            var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdRaw, out var userId))
            {
                var currentUser = await _db.Users
                    .AsNoTracking()
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                if (currentUser is not null && !User.IsInRole(RoleCodes.ExternalBeneficiary))
                {
                    model.IsInternalApplicant = true;
                    model.ApplicantFullName = currentUser.FullName;
                    model.ApplicantDepartmentName = currentUser.Department?.Name;
                }
            }

            await PopulateLookupsAsync(model);
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> Create(IdeaCreateViewModel model, string action, List<IFormFile>? attachments, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await PopulateLookupsAsync(model);
                return View(model);
            }

            var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdRaw, out var userId))
            {
                _logger.LogWarning("Authenticated user has no parsable id claim.");
                return Challenge();
            }

            var isSaveDraft = string.Equals(action, "SaveDraft", StringComparison.OrdinalIgnoreCase);
            var isSubmit = string.Equals(action, "Submit", StringComparison.OrdinalIgnoreCase);
            if (!isSaveDraft && !isSubmit)
            {
                ModelState.AddModelError(string.Empty, "إجراء غير معروف.");
                await PopulateLookupsAsync(model);
                return View(model);
            }

            var idea = new InnovationIdea
            {
                Id = Guid.NewGuid(),
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                ProblemStatement = NullIfBlank(model.ProblemStatement),
                ProposedSolution = NullIfBlank(model.ProposedSolution),
                ExpectedBenefits = NullIfBlank(model.ExpectedBenefits),
                ExpectedImpactOther = NullIfBlank(model.ExpectedImpactOther),
                TargetAudienceOther = NullIfBlank(model.TargetAudienceOther),
                UsesEmergingTech = model.UsesEmergingTech,
                TechnologyOther = NullIfBlank(model.TechnologyOther),
                InnovationDomainId = model.InnovationDomainId,
                ExpectedImpactId = model.ExpectedImpactId,
                TargetAudienceId = model.TargetAudienceId,
                ApplicantUserId = userId,
                ApplicantDepartmentId = User.FindFirst("ibtikar_department_id")?.Value is { } depStr && Guid.TryParse(depStr, out var depGuid) ? depGuid : null,
                IsDraft = isSaveDraft,
                CreatedAt = DateTime.UtcNow,
                SubmittedAt = isSubmit ? DateTime.UtcNow : null
            };

            if (isSubmit)
            {
                idea.ReferenceNumber = await NextReferenceNumberAsync(ct);
                var newStatus = await _db.IdeaStatuses.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Code == IdeaStatusCodes.New, ct);
                if (newStatus is null)
                {
                    _logger.LogError("IdeaStatus with code 'new' is missing from seed.");
                    ModelState.AddModelError(string.Empty, "حالة الطلب غير مهيأة. تواصل مع مدير النظام.");
                    await PopulateLookupsAsync(model);
                    return View(model);
                }
                idea.CurrentStatusId = newStatus.Id;
            }
            else
            {
                idea.ReferenceNumber = string.Empty;
                var draftStatus = await _db.IdeaStatuses.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Code == IdeaStatusCodes.New, ct);
                idea.CurrentStatusId = draftStatus?.Id ?? Guid.Empty;
            }

            _db.InnovationIdeas.Add(idea);

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist idea for user {UserId}", userId);
                ModelState.AddModelError(string.Empty, "تعذّر حفظ الطلب. حاول مرة أخرى.");
                await PopulateLookupsAsync(model);
                return View(model);
            }

            if (isSubmit && !string.IsNullOrEmpty(idea.ReferenceNumber))
            {
                var attachError = await TrySaveAttachmentsAsync(idea.Id, userId, attachments, ct);
                if (attachError is not null)
                {
                    ModelState.AddModelError(string.Empty, attachError);
                    await PopulateLookupsAsync(model);
                    return View(model);
                }

                try
                {
                    await _procedureGateway.NotifyAsync(idea.ReferenceNumber, ct);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx,
                        "Procedure notify failed for {Reference} but idea is persisted.",
                        idea.ReferenceNumber);
                }

                return RedirectToAction(nameof(Submitted), new { referenceNumber = idea.ReferenceNumber });
            }

            TempData["IdeaDraftSaved"] = "تم حفظ المسودة. يمكنك العودة لإكمالها لاحقاً من صفحة طلباتي.";
            return RedirectToAction("Index", "MyRequests");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Submitted(string referenceNumber, CancellationToken ct)
        {            if (string.IsNullOrWhiteSpace(referenceNumber))
            {
                return RedirectToAction(nameof(Create));
            }

            var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdRaw, out var userId))
            {
                return Challenge();
            }

            var idea = await _db.InnovationIdeas
                .AsNoTracking()
                .Include(i => i.CurrentStatus)
                .Include(i => i.InnovationDomain)
                .Where(i => i.ReferenceNumber == referenceNumber && i.ApplicantUserId == userId)
                .Select(i => new IdeaSuccessVm(
                    i.ReferenceNumber,
                    i.Title,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#0d6efd",
                    i.InnovationDomain != null ? i.InnovationDomain.Name : "—",
                    i.SubmittedAt ?? i.CreatedAt))
                .FirstOrDefaultAsync(ct);

            if (idea is null)
            {
                return RedirectToAction("Index", "MyRequests");
            }

            return View(idea);
        }

        private async Task<string?> TrySaveAttachmentsAsync(Guid ideaId, Guid userId, List<IFormFile>? attachments, CancellationToken ct)
        {
            if (attachments is null || attachments.Count == 0) return null;
            foreach (var file in attachments)
            {
                if (file is null || file.Length == 0) continue;
                var result = await _attachments.SaveAsync(ideaId, userId, file, ct);
                if (!result.Success)
                {
                    return result.Error;
                }
            }
            return null;
        }

        private async Task<string> NextReferenceNumberAsync(CancellationToken ct)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"IBT-{year}-";
            var maxInYear = await _db.InnovationIdeas
                .Where(i => i.ReferenceNumber.StartsWith(prefix))
                .Select(i => i.ReferenceNumber)
                .ToListAsync(ct);

            int maxSeq = 0;
            foreach (var refn in maxInYear)
            {
                var tail = refn.Substring(prefix.Length);
                if (int.TryParse(tail, out var n) && n > maxSeq) maxSeq = n;
            }
            return $"{prefix}{(maxSeq + 1):D4}";
        }

        private static string? NullIfBlank(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private async Task PopulateLookupsAsync(IdeaCreateViewModel model)
        {
            ViewBag.InnovationDomains = await _db.InnovationDomains
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            ViewBag.ExpectedImpacts = await _db.ExpectedImpacts
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            ViewBag.TargetAudiences = await _db.TargetAudiences
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            ViewBag.Technologies = await _db.Technologies
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();
        }
    }

    public record IdeaSuccessVm(
        string ReferenceNumber,
        string Title,
        string StatusName,
        string StatusColor,
        string DomainName,
        DateTime SubmittedAt);
}
