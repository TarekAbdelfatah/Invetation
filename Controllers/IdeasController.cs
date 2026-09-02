using Ibtikar.DTOs.Ideas;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Interfaces;
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

        [Authorize]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            try
            {
                var dtos = await _ideaService.GetLatestAsync(50, ct);
                var items = dtos.Select(ToListItemVm).ToList();
                return View(items);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ideas index fallback (database unavailable): {Message}", ex.Message);
                ViewBag.DatabaseError = "تعذر الاتصال بقاعدة البيانات. حاول لاحقاً.";
                return View(Array.Empty<IdeaListItemVm>());
            }
        }

        [Authorize]
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Create(Guid? draftId, CancellationToken ct)
        {
            var model = new IdeaCreateViewModel
            {
                CurrentDraftId = draftId ?? Guid.NewGuid()
            };

            var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdRaw, out var userId))
            {
                if (draftId.HasValue && draftId.Value != Guid.Empty)
                {
                    var draft = await _ideaService.GetDraftForEditAsync(draftId.Value, userId, Array.Empty<Guid>(), ct);
                    if (draft is null)
                    {
                        return RedirectToAction("Index", "MyRequests");
                    }
                    CopyFromDraft(model, draft);
                    model.IsResumingDraft = true;
                    model.IsAlreadySubmitted = !string.IsNullOrWhiteSpace(draft.ReferenceNumber);
                }

                var currentUser = await _ideaService.GetUserSummaryAsync(userId, ct);
                if (currentUser is not null && BeneficiaryType.IsInternal(User))
                {
                    model.IsInternalApplicant = true;
                    model.ApplicantFullName = currentUser.FullName;
                    model.ApplicantDepartmentName = currentUser.DepartmentName;
                }
            }

            await PopulateLookupsAsync(model, ct);
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> Create(IdeaCreateViewModel model, string action, List<IFormFile>? attachments, CancellationToken ct)
        {
            var isSaveDraft = string.Equals(action, "SaveDraft", StringComparison.OrdinalIgnoreCase);
            var isSubmit = string.Equals(action, "Submit", StringComparison.OrdinalIgnoreCase);
            if (!isSaveDraft && !isSubmit)
            {
                ModelState.AddModelError(string.Empty, "إجراء غير معروف.");
                await PopulateLookupsAsync(model, ct);
                return View(model);
            }

            // Re-run validation now that we know whether the user is submitting or saving a draft,
            // so conditional Required rules (submit only) are applied correctly.
            model.IsSubmit = isSubmit;
            ModelState.ClearValidationState(nameof(IdeaCreateViewModel));
            TryValidateModel(model);

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

            var departmentId = User.FindFirst("ibtikar_department_id")?.Value is { } depStr
                && Guid.TryParse(depStr, out var depGuid)
                ? depGuid
                : (Guid?)null;

            var existingDraftId = await ResolveExistingDraftIdAsync(model, userId, ct);

            var request = ToCreateRequest(model);
            var result = await _ideaService.CreateIdeaAsync(
                request,
                userId,
                departmentId,
                isSaveDraft,
                model.CurrentDraftId,
                existingDraftId,
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

            var dto = await _ideaService.GetDetailsAsync(referenceNumber, userId, ct);

            if (dto is null)
            {
                return RedirectToAction("Index", "MyRequests");
            }

            var vm = ToSuccessVm(dto);
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

        private static CreateIdeaRequestDto ToCreateRequest(IdeaCreateViewModel model)
            => new(
                model.Title,
                model.Description,
                model.ProblemStatement,
                model.ProposedSolution,
                model.ExpectedBenefits,
                model.InnovationDomainId,
                model.ExpectedImpactId,
                model.ExpectedImpactOther,
                model.TargetAudienceId,
                model.TargetAudienceOther,
                model.UsesEmergingTech,
                model.TechnologyIds,
                model.TechnologyOther,
                model.RequiredResources);

        private static IdeaListItemVm ToListItemVm(IdeaSummaryDto dto)
            => new(
                dto.Id,
                dto.ReferenceNumber,
                dto.Title,
                dto.TitleDisplay,
                dto.StatusName,
                dto.StatusColor,
                dto.DomainName,
                dto.DepartmentName,
                dto.SubmittedAt,
                dto.CreatedAt);

        private async Task<Guid?> ResolveExistingDraftIdAsync(IdeaCreateViewModel model, Guid userId, CancellationToken ct)
        {
            if (model.IsResumingDraft && model.CurrentDraftId.HasValue && model.CurrentDraftId.Value != Guid.Empty)
            {
                var existing = await _ideaService.GetDraftForEditAsync(model.CurrentDraftId.Value, userId, Array.Empty<Guid>(), ct);
                if (existing is not null) return model.CurrentDraftId.Value;
            }
            return null;
        }

        private static void CopyFromDraft(IdeaCreateViewModel model, IdeaDetailsForEditDto draft)
        {
            model.Title = draft.Title;
            model.Description = draft.Description;
            model.ProblemStatement = draft.ProblemStatement;
            model.ProposedSolution = draft.ProposedSolution;
            model.ExpectedBenefits = draft.ExpectedBenefits;
            model.RequiredResources = draft.RequiredResources;
            model.InnovationDomainId = draft.InnovationDomainId == Guid.Empty ? null : draft.InnovationDomainId;
            model.ExpectedImpactId = draft.ExpectedImpactId;
            model.ExpectedImpactOther = draft.ExpectedImpactOther;
            model.TargetAudienceId = draft.TargetAudienceId;
            model.TargetAudienceOther = draft.TargetAudienceOther;
            model.UsesEmergingTech = draft.UsesEmergingTech;
            model.TechnologyIds = draft.TechnologyIds.ToList();
            model.TechnologyOther = draft.TechnologyOther;
        }

        private static IdeaSuccessVm ToSuccessVm(IdeaDetailsDto dto)
            => new(
                dto.ReferenceNumber,
                dto.Title,
                dto.StatusName,
                dto.StatusColor,
                dto.DomainName,
                dto.SubmittedAt);
    }
}