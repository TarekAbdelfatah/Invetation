using Ibtikar.DTOs.Committee;
using Ibtikar.Services.Interfaces;
using Ibtikar.Services.Helpers;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [IbtikarAuthorize(RoleCodes.InnovationCommitteeMember)]
    public class CommitteeDashboardController : Controller
    {
        private readonly ICommitteeDashboardService _dashboardService;
        private readonly IDelegationService _delegations;
        private readonly ILogger<CommitteeDashboardController> _logger;

        public CommitteeDashboardController(
            ICommitteeDashboardService dashboardService,
            IDelegationService delegations,
            ILogger<CommitteeDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _delegations = delegations;
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

        [HttpGet("Committee/Votes")]
        public async Task<IActionResult> Votes(CancellationToken ct)
        {
            var dto = await _dashboardService.GetVotesAsync(ResolveUserId(), ct);
            if (dto is null) return Forbid();

            var vm = new CommitteeVotesVm
            {
                Items = dto.Items.Select(i => new CommitteeVoteRowVm(
                    i.IdeaId, i.Reference, i.Title,
                    i.StatusCode, i.StatusName, i.StatusColor,
                    i.HasVoted, i.MyVote)).ToList()
            };
            return View(vm);
        }

        [HttpPost("Committee/Votes")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitVote(Guid ideaId, string decision, CancellationToken ct)
        {
            var submission = new CommitteeVoteSubmitDto(ideaId, decision);
            var result = await _dashboardService.SubmitVoteAsync(ResolveUserId(), submission, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message ?? "حدث خطأ.";
            TempData["AlertType"] = result.Success ? "success" : "danger";

            return RedirectToAction(nameof(Votes));
        }

        [HttpGet("Committee/Decision/{id:guid}")]
        public async Task<IActionResult> Decision(Guid id, CancellationToken ct)
        {
            var dto = await _dashboardService.GetDecisionAsync(ResolveUserId(), id, ct);
            if (dto is null) return Forbid();

            var vm = new CommitteeDecisionVm
            {
                IdeaId = dto.IdeaId,
                Reference = dto.Reference,
                Title = dto.Title,
                CombinedAverage = dto.CombinedAverage,
                CanAccept = dto.CanAccept,
                ExtraConfirmWarning = dto.ExtraConfirmWarning
            };
            return View(vm);
        }

        [HttpPost("Committee/Accept")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(Guid ideaId, bool extraConfirmed, CancellationToken ct)
        {
            var result = await _dashboardService.AcceptAsync(ResolveUserId(), ideaId, extraConfirmed, ct);

            if (!result.Success)
            {
                TempData["AlertError"] = result.Message;
                TempData["AlertType"] = "danger";
                return RedirectToAction(nameof(Decision), new { id = ideaId });
            }

            TempData["AlertMessage"] = result.Message;
            TempData["AlertType"] = "success";
            return RedirectToAction("Index", "CommitteeDashboard");
        }

        [HttpPost("Committee/Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(CommitteeRejectVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var dto = await _dashboardService.GetDecisionAsync(ResolveUserId(), vm.IdeaId, ct);
                if (dto is null) return Forbid();
                return View("Decision", new CommitteeDecisionVm
                {
                    IdeaId = dto.IdeaId,
                    Reference = dto.Reference,
                    Title = dto.Title,
                    CombinedAverage = dto.CombinedAverage,
                    CanAccept = dto.CanAccept,
                    ExtraConfirmWarning = dto.ExtraConfirmWarning,
                    Reason = vm.Reason,
                    ShowRejectBox = true
                });
            }

            var result = await _dashboardService.RejectAsync(ResolveUserId(), vm.IdeaId, vm.Reason ?? string.Empty, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message ?? "حدث خطأ.";
            TempData["AlertType"] = result.Success ? "success" : "danger";

            if (!result.Success)
                return RedirectToAction(nameof(Decision), new { id = vm.IdeaId });

            return RedirectToAction("Index", "CommitteeDashboard");
        }

        [HttpPost("Committee/ReturnForDevelopment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnForDevelopment(Guid ideaId, CancellationToken ct)
        {
            var result = await _dashboardService.ReturnForDevelopmentAsync(ResolveUserId(), ideaId, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message ?? "حدث خطأ.";
            TempData["AlertType"] = result.Success ? "success" : "danger";

            if (!result.Success)
                return RedirectToAction(nameof(Decision), new { id = ideaId });

            return RedirectToAction("Index", "CommitteeDashboard");
        }

        [HttpGet("Committee/Delegations")]
        public async Task<IActionResult> Delegations(CancellationToken ct)
        {
            var vm = await BuildDelegationsVmAsync(ResolveUserId(), ct);
            if (vm is null) return Forbid();
            return View(vm);
        }

        [HttpPost("Committee/Delegations")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDelegation(CommitteeDelegationCreateVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var delegationsVm = await BuildDelegationsVmAsync(ResolveUserId(), ct);
                if (delegationsVm is null) return Forbid();
                delegationsVm.DelegateMemberUserId = vm.DelegateMemberUserId;
                delegationsVm.StartAt = vm.StartAt;
                delegationsVm.EndAt = vm.EndAt;
                return View(nameof(Delegations), delegationsVm);
            }

            var userId = ResolveUserId();
            var committeeId = await _delegations.GetCommitteeIdForHeadAsync(userId, ct);
            if (committeeId is null) return Forbid();

            var result = await _delegations.AddAsync(committeeId.Value, userId, vm.DelegateMemberUserId!.Value, vm.StartAt!.Value, vm.EndAt!.Value, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message;
            TempData["AlertType"] = result.Success ? "success" : "danger";
            return RedirectToAction(nameof(Delegations));
        }

        private async Task<CommitteeDelegationsVm?> BuildDelegationsVmAsync(Guid userId, CancellationToken ct)
        {
            var committeeId = await _delegations.GetCommitteeIdForHeadAsync(userId, ct);
            if (committeeId is null)
            {
                var memberCommitteeId = await _delegations.GetCommitteeIdForMemberAsync(userId, ct);
                if (memberCommitteeId is null) return null;

                var active = await _delegations.GetActiveAsync(memberCommitteeId.Value, ct);
                return new CommitteeDelegationsVm
                {
                    IsHead = false,
                    DelegateName = active?.DelegateMember?.FullName,
                    ActiveFrom = active?.StartAt,
                    ActiveTo = active?.EndAt,
                    Rows = (await _delegations.GetDelegationsAsync(memberCommitteeId.Value, ct))
                        .Select(ToDelegationRowVm)
                        .ToList()
                };
            }

            var candidates = await _delegations.GetDelegateCandidatesAsync(committeeId.Value, ct);
            var delegations = await _delegations.GetDelegationsAsync(committeeId.Value, ct);

            return new CommitteeDelegationsVm
            {
                IsHead = true,
                Candidates = candidates.Select(c => new DelegationMemberOptionVm(c.UserId, c.FullName, c.Username)).ToList(),
                Rows = delegations.Select(ToDelegationRowVm).ToList()
            };
        }

        [HttpPost("Committee/Delegations/Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelDelegation(Guid delegationId, CancellationToken ct)
        {
            var userId = ResolveUserId();
            var committeeId = await _delegations.GetCommitteeIdForHeadAsync(userId, ct);
            if (committeeId is null) return Forbid();

            var result = await _delegations.CancelAsync(committeeId.Value, userId, delegationId, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message;
            TempData["AlertType"] = result.Success ? "success" : "danger";
            return RedirectToAction(nameof(Delegations));
        }

        private static DelegationRowVm ToDelegationRowVm(DelegationRowDto d)
            => new(d.Id, d.DelegateName, d.StartAt, d.EndAt, d.IsActive);

        private Guid ResolveUserId()
        {
            var raw = User.FindFirst(RoleCodes.UserIdClaim)?.Value;
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
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
                return RedirectToAction("Index", "CommitteeDashboard");

            return RedirectToAction(nameof(Assess), new { id = ideaId });
        }

        private string? ResolveCommitteeName()
            => User.FindFirst("ibtikar_full_name")?.Value;
    }
}
