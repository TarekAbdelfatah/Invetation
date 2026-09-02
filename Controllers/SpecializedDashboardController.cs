using Ibtikar.DTOs.PartnerDashboard;
using Ibtikar.DTOs.SpecializedDashboard;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Interfaces;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [IbtikarAuthorize(RoleCodes.SpecializedDepartment, RoleCodes.PartnerDepartment)]
    public class SpecializedDashboardController : Controller
    {
        private readonly ISpecializedDashboardService _service;
        private readonly IPartnerDashboardService _partnerService;

        public SpecializedDashboardController(
            ISpecializedDashboardService service,
            IPartnerDashboardService partnerService)
        {
            _service = service;
            _partnerService = partnerService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var departmentId = ResolveDepartmentId();
            var dto = await _service.GetSnapshotAsync(departmentId, ct);
            var advisoryDto = await _partnerService.GetSnapshotAsync(departmentId, ct);
            var advisoryInbox = await _partnerService.GetInboxAsync(departmentId, ct);

            var vm = new SpecializedDashboardVm
            {
                UnderStudy = dto?.UnderStudy ?? 0,
                SentToPartner = dto?.SentToPartner ?? 0,
                SentToExecution = dto?.SentToExecution ?? 0,
                RejectedAfterRouting = dto?.RejectedAfterRouting ?? 0,
                AdvisoryPending = advisoryDto?.PendingAssignments ?? 0,
                AdvisoryLate = advisoryDto?.OverdueLate ?? 0,
                AdvisorySubmitted = advisoryDto?.SubmittedThisCycle ?? 0,
                DepartmentName = ResolveDepartmentName(),
                AdvisoryItems = (advisoryInbox?.Items ?? new List<PartnerAssignmentRowDto>())
                    .Select(i => new PartnerAssignmentRowVm(
                        i.AssignmentId, i.IdeaId, i.IdeaReference, i.IdeaTitle,
                        i.ApplicantName, i.SourceDepartmentName,
                        i.SentAt, i.RespondedAt,
                        i.Status, i.IsLate, i.IsPending, i.IsReturned, i.DaysOpen))
                    .ToList()
            };
            return View(vm);
        }

        [HttpGet("SpecializedDashboard/Referrals")]
        public async Task<IActionResult> Referrals(string? status, CancellationToken ct)
        {
            var departmentId = ResolveDepartmentId();
            var dto = await _service.GetReferralsAsync(departmentId, status, ct);
            if (dto is null) return Forbid();

            var vm = new SpecializedReferralsVm
            {
                StatusFilter = dto.StatusFilter ?? string.Empty,
                DepartmentName = ResolveDepartmentName(),
                Items = dto.Items.Select(i => new SpecializedReferralRowVm(
                    i.Id, i.Reference, i.Title,
                    i.StatusCode, i.StatusName, i.StatusColor,
                    i.AssignedAt, i.StayDays, i.ApplicantName, i.IsOverdue)).ToList()
            };
            return View("Referrals", vm);
        }

        [HttpGet("SpecializedDashboard/Details/{id:guid}")]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var dto = await _service.GetDetailsAsync(ResolveDepartmentId(), id, ct);
            if (dto is null) return Forbid();

            var vm = new SpecializedDetailsVm
            {
                Id = dto.Id,
                Reference = dto.Reference,
                Title = dto.Title,
                Description = dto.Description,
                ProblemStatement = dto.ProblemStatement,
                ProposedSolution = dto.ProposedSolution,
                ExpectedBenefits = dto.ExpectedBenefits,
                DomainName = dto.DomainName,
                ExpectedImpactName = dto.ExpectedImpactName,
                TargetAudienceName = dto.TargetAudienceName,
                ApplicantName = dto.ApplicantName,
                ApplicantDepartmentName = dto.ApplicantDepartmentName,
                StatusName = dto.StatusName,
                StatusColor = dto.StatusColor,
                StatusCode = dto.StatusCode,
                SubmittedAt = dto.SubmittedAt,
                AssignedAt = dto.AssignedAt,
                Attachments = dto.Attachments.Select(a => new SpecializedAttachmentVm(a.Id, a.FileName, a.SizeBytes, a.UploadedAt)).ToList(),
                History = dto.History.Select(h => new SpecializedHistoryRowVm(h.ChangedAt, h.FromStatus, h.ToStatus, h.By, h.Note)).ToList()
            };
            return View(vm);
        }

        [HttpGet("SpecializedDashboard/Assess/{id:guid}")]
        public async Task<IActionResult> Assess(Guid id, CancellationToken ct)
        {
            var dto = await _service.GetAssessVmAsync(ResolveDepartmentId(), id, ct);
            if (dto is null) return Forbid();

            var vm = new SpecializedAssessVm
            {
                IdeaId = dto.IdeaId,
                Reference = dto.Reference,
                Title = dto.Title,
                StatusName = dto.StatusName,
                StatusColor = dto.StatusColor,
                IsDraftSaved = dto.IsDraftSaved,
                IsLocked = dto.IsLocked,
                DraftHeaderId = dto.DraftHeaderId,
                DraftSavedAt = dto.DraftSavedAt,
                TotalScore = dto.TotalScore,
                Comment = dto.Comment,
                Criteria = dto.Criteria.Select(c => new SpecializedCriterionVm(c.Id, c.Code, c.Name, c.Description, c.DisplayOrder)).ToList(),
                Lines = dto.Lines.Select(l => new SpecializedAssessmentLineVm(l.CriterionId, l.CriterionCode, l.CriterionName, l.Score, l.Comment)).ToList()
            };
            return View(vm);
        }

        [HttpPost("SpecializedDashboard/Assess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAssess(
            Guid ideaId,
            Guid? headerId,
            string? comment,
            bool saveDraft,
            IFormCollection form,
            CancellationToken ct)
        {
            var scores = new List<SpecializedScoreInputDto>();
            foreach (var key in form.Keys.Where(k => k.StartsWith("score_")))
            {
                if (!Guid.TryParse(key.AsSpan(6), out var criterionId)) continue;
                if (!int.TryParse(form[key], out var score)) continue;
                var commentKey = $"comment_{criterionId}";
                var c = form.TryGetValue(commentKey, out var cv) ? cv.ToString() : null;
                scores.Add(new SpecializedScoreInputDto(criterionId, score, c));
            }

            var submission = new SpecializedAssessmentSubmissionDto(ideaId, headerId, scores, comment, saveDraft);
            var result = await _service.SaveAssessmentAsync(ResolveDepartmentId(), ResolveUserId(), submission, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message ?? "حدث خطأ.";
            TempData["AlertType"] = result.Success ? "success" : "danger";

            if (result.Success && !saveDraft)
                return RedirectToAction(nameof(SendToCommittee), new { id = ideaId });

            return RedirectToAction(nameof(Assess), new { id = ideaId });
        }

        [HttpGet("SpecializedDashboard/Send/{id:guid}")]
        public async Task<IActionResult> SendToCommittee(Guid id, CancellationToken ct)
        {
            var dto = await _service.GetSendToCommitteeSummaryAsync(ResolveDepartmentId(), id, ct);
            if (dto is null) return Forbid();

            var vm = new SpecializedSendToCommitteeVm
            {
                IdeaId = dto.IdeaId,
                Reference = dto.Reference,
                TotalCriteria = dto.TotalCriteria,
                CompletedCriteria = dto.CompletedCriteria,
                UnrepliedPartners = dto.UnrepliedPartners,
                CanSend = dto.CanSend,
                WarningMessage = dto.WarningMessage
            };
            return View(vm);
        }

        [HttpPost("SpecializedDashboard/Send")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmSendToCommittee(Guid ideaId, bool skipPartnerWarning, CancellationToken ct)
        {
            var result = await _service.SendToCommitteeAsync(ResolveDepartmentId(), ResolveUserId(), ideaId, skipPartnerWarning, ct);

            if (!result.Success)
            {
                if (result.RequiresConfirmation)
                {
                    TempData["PartnerWarning"] = result.Message;
                    return RedirectToAction(nameof(SendToCommittee), new { id = ideaId });
                }
                TempData["AlertError"] = result.Message;
                return RedirectToAction(nameof(Assess), new { id = ideaId });
            }

            TempData["AlertMessage"] = result.Message;
            TempData["AlertType"] = "success";
            return RedirectToAction(nameof(Referrals), new { status = (string?)null });
        }

        [HttpGet("SpecializedDashboard/Request/{id:guid}")]
        public new async Task<IActionResult> Request(Guid id, CancellationToken ct)
        {
            var vm = await BuildRequestVmAsync(id, ct);
            if (vm is null) return Forbid();
            return View(vm);
        }

        [HttpPost("SpecializedDashboard/Request")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmRequest(SpecializedRequestSubmitVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var requestVm = await BuildRequestVmAsync(vm.IdeaId, ct);
                if (requestVm is null) return Forbid();
                requestVm.PartnerIds = vm.PartnerIds;
                requestVm.SelectedPartnerIds = vm.PartnerIds;
                requestVm.Note = vm.Note;
                return View("Request", requestVm);
            }

            var submission = new SpecializedRequestSubmissionDto(vm.IdeaId, vm.PartnerIds, vm.Note);
            var result = await _service.RequestPartnerOpinionsAsync(ResolveDepartmentId(), ResolveUserId(), submission, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message ?? "حدث خطأ.";
            TempData["AlertType"] = result.Success ? "success" : "danger";
            return RedirectToAction(nameof(PartnerOpinion), new { id = vm.IdeaId });
        }

        private async Task<SpecializedRequestVm?> BuildRequestVmAsync(Guid id, CancellationToken ct)
        {
            var dto = await _service.GetRequestVmAsync(ResolveDepartmentId(), id, ct);
            if (dto is null) return null;

            return new SpecializedRequestVm
            {
                IdeaId = dto.IdeaId,
                Reference = dto.Reference,
                Title = dto.Title,
                AvailablePartners = dto.AvailablePartners.Select(p => new SpecializedPartnerOptionVm(p.Id, p.Name, p.Code)).ToList(),
                AlreadyAssigned = dto.AlreadyAssigned.Select(p => new SpecializedPartnerOptionVm(p.Id, p.Name, p.Code)).ToList()
            };
        }

        [HttpGet("SpecializedDashboard/Partners/{id:guid}")]
        public async Task<IActionResult> PartnerOpinion(Guid id, CancellationToken ct)
        {
            var dto = await _service.GetPartnerOpinionAsync(ResolveDepartmentId(), id, ct);
            if (dto is null) return Forbid();

            var vm = new SpecializedPartnerOpinionVm
            {
                IdeaId = dto.IdeaId,
                Reference = dto.Reference,
                Title = dto.Title,
                Rows = dto.Rows.Select(r => new SpecializedPartnerFollowUpVm(
                    r.AssignmentId, r.IdeaId, r.IdeaReference, r.IdeaTitle,
                    r.PartnerDepartmentName, r.Status, r.StatusBadgeClass,
                    r.SentAt, r.RespondedAt, r.DaysOpen, r.IsLate, r.Note)).ToList()
            };
            return View(vm);
        }

        private Guid? ResolveDepartmentId()
        {
            var raw = User.FindFirst(RoleCodes.DepartmentIdClaim)?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }

        private string? ResolveDepartmentName()
            => User.FindFirst(RoleCodes.DepartmentNameClaim)?.Value;

        private Guid ResolveUserId()
        {
            var raw = User.FindFirst(RoleCodes.UserIdClaim)?.Value;
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }
    }

    internal static class SpecializedScoreInputDtoExtensions
    {
        public static SpecializedScoreInputDto WithCriterionId(this SpecializedScoreInputDto dto, Guid id)
            => new(id, 0, null);
    }
}