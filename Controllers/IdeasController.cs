using Ibtikar.Models;
using Ibtikar.Services.Ideas;
using Ibtikar.Services.Security;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    public class IdeasController : Controller
    {
        private readonly IIdeaService _ideaService;
        private readonly ILogger<IdeasController> _logger;

        public IdeasController(IIdeaService ideaService, ILogger<IdeasController> logger)
        {
            _ideaService = ideaService;
            _logger = logger;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            try
            {
                var ideas = await _ideaService.GetLatestAsync(50, ct);
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
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var model = new IdeaCreateViewModel();

            var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdRaw, out var userId))
            {
                var currentUser = await _ideaService.GetUserWithDepartmentAsync(userId, ct);
                if (currentUser is not null && !User.IsInRole(RoleCodes.ExternalBeneficiary))
                {
                    model.IsInternalApplicant = true;
                    model.ApplicantFullName = currentUser.FullName;
                    model.ApplicantDepartmentName = currentUser.Department?.Name;
                }
            }

            await PopulateLookupsAsync(model, ct);
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
                await PopulateLookupsAsync(model, ct);
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
                await PopulateLookupsAsync(model, ct);
                return View(model);
            }

            var departmentId = User.FindFirst("ibtikar_department_id")?.Value is { } depStr
                && Guid.TryParse(depStr, out var depGuid)
                ? depGuid
                : (Guid?)null;

            var result = await _ideaService.CreateIdeaAsync(
                model,
                userId,
                departmentId,
                isSaveDraft,
                attachments,
                ct);

            if (!result.Success)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
                await PopulateLookupsAsync(model, ct);
                return View(model);
            }

            if (result.IsSubmitted)
            {
                return RedirectToAction(nameof(Submitted), new { referenceNumber = result.ReferenceNumber });
            }

            TempData["IdeaDraftSaved"] = "تم حفظ المسودة. يمكنك العودة لإكمالها لاحقاً من صفحة طلباتي.";
            return RedirectToAction("Index", "MyRequests");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Submitted(string referenceNumber, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(referenceNumber))
            {
                return RedirectToAction(nameof(Create));
            }

            var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdRaw, out var userId))
            {
                return Challenge();
            }

            var vm = await _ideaService.GetSuccessVmByReferenceAsync(referenceNumber, userId, ct);

            if (vm is null)
            {
                return RedirectToAction("Index", "MyRequests");
            }

            return View(vm);
        }

        private async Task PopulateLookupsAsync(IdeaCreateViewModel model, CancellationToken ct)
        {
            var lookups = await _ideaService.GetLookupsAsync(ct);
            ViewBag.InnovationDomains = new SelectList(lookups.InnovationDomains, "Value", "Text");
            ViewBag.ExpectedImpacts = new SelectList(lookups.ExpectedImpacts, "Value", "Text");
            ViewBag.TargetAudiences = new SelectList(lookups.TargetAudiences, "Value", "Text");
            ViewBag.Technologies = new SelectList(lookups.Technologies, "Value", "Text");
        }
    }
}
