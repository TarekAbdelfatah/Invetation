using Ibtikar.DTOs.Committee;
using Ibtikar.Services.Committee;
using Ibtikar.Services.Security;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.InnovationCommitteeMember)]
    public class CommitteeController : Controller
    {
        private readonly ICommitteeDashboardService _dashboardService;
        private readonly ILogger<CommitteeController> _logger;

        public CommitteeController(
            ICommitteeDashboardService dashboardService,
            ILogger<CommitteeController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        [HttpGet("Committee")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            try
            {
                var userId = ResolveUserId();
                if (!await _dashboardService.IsActiveCommitteeMemberAsync(userId, ct))
                {
                    return Forbid();
                }

                var dto = await _dashboardService.GetSnapshotAsync(userId, ct);
                var vm = new CommitteeDashboardVm
                {
                    UnderStudy = dto.UnderStudy,
                    UnderVoting = dto.UnderVoting,
                    Accepted = dto.Accepted,
                    Rejected = dto.Rejected,
                    CommitteeName = ResolveCommitteeName()
                };
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Committee index fallback: {Message}", ex.Message);
                ViewBag.DatabaseError = ex.Message;
                return View(new CommitteeDashboardVm());
            }
        }

        [HttpGet("Committee/Referrals")]
        public async Task<IActionResult> Referrals(string? status, CancellationToken ct)
        {
            var userId = ResolveUserId();
            var dto = await _dashboardService.GetReferralsAsync(userId, status ?? string.Empty, ct);
            if (dto is null) return Forbid();

            var vm = new CommitteeReferralsVm
            {
                StatusFilter = dto.StatusFilter ?? string.Empty,
                CommitteeName = ResolveCommitteeName(),
                Items = dto.Items.Select(i => new CommitteeReferralRowVm(
                    i.IdeaId, i.Reference, i.Title,
                    i.StatusCode, i.StatusName, i.StatusColor,
                    i.ApplicantName, i.ApplicantDepartmentName,
                    i.ReferredAt, i.StayDays, i.IsOverdue)).ToList()
            };
            return View("Referrals", vm);
        }

        [HttpGet("Committee/Assess/{id:guid}")]
        public async Task<IActionResult> Assess(Guid id, CancellationToken ct)
        {
            var dto = await _dashboardService.GetAssessAsync(ResolveUserId(), id, ct);
            if (dto is null) return Forbid();

            var vm = new CommitteeAssessVm
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
                DepartmentPercent = dto.DepartmentPercent,
                CommitteePercent = dto.CommitteePercent,
                CombinedAverage = dto.CombinedAverage,
                Criteria = dto.Criteria.Select(c => new CommitteeCriterionVm(c.Id, c.Code, c.Name, c.Description, c.DisplayOrder)).ToList(),
                Lines = dto.Lines.Select(l => new CommitteeAssessLineVm(l.CriterionId, l.CriterionCode, l.CriterionName, l.Score, l.Comment)).ToList()
            };
            return View(vm);
        }

        [HttpPost("Committee/Assess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAssess(
            Guid ideaId,
            Guid? headerId,
            string? comment,
            bool saveDraft,
            IFormCollection form,
            CancellationToken ct)
        {
            var scores = new List<CommitteeScoreInputDto>();
            foreach (var key in form.Keys.Where(k => k.StartsWith("score_")))
            {
                if (!Guid.TryParse(key.AsSpan(6), out var criterionId)) continue;
                if (!int.TryParse(form[key], out var score)) continue;
                var commentKey = $"comment_{criterionId}";
                var c = form.TryGetValue(commentKey, out var cv) ? cv.ToString() : null;
                scores.Add(new CommitteeScoreInputDto(criterionId, score, c));
            }

            var submission = new CommitteeAssessmentSubmissionDto(ideaId, headerId, scores, comment, saveDraft);
            var result = await _dashboardService.SaveAssessmentAsync(ResolveUserId(), submission, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message ?? "حدث خطأ.";
            TempData["AlertType"] = result.Success ? "success" : "danger";

            if (result.Success && !saveDraft)
                return RedirectToAction("Index", "Committee");

            return RedirectToAction(nameof(Assess), new { id = ideaId });
        }

        private Guid ResolveUserId()
        {
            var raw = User.FindFirst(RoleCodes.UserIdClaim)?.Value;
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }

        private string? ResolveCommitteeName()
            => User.FindFirst("ibtikar_full_name")?.Value;
    }
}
