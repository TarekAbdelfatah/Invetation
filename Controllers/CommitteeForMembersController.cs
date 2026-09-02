using Ibtikar.DTOs.Committee;
using Ibtikar.Services.Interfaces;
using Ibtikar.Services.Helpers;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.InnovationCommitteeMember)]
    public class CommitteeForMembersController : Controller
    {
        private readonly ICommitteeDashboardService _dashboardService;
        private readonly IDelegationService _delegations;
        private readonly ILogger<CommitteeForMembersController> _logger;

        public CommitteeForMembersController(
            ICommitteeDashboardService dashboardService,
            IDelegationService delegations,
            ILogger<CommitteeForMembersController> logger)
        {
            _dashboardService = dashboardService;
            _delegations = delegations;
            _logger = logger;
        }

        [HttpGet("CommitteeForMembers")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index(int? page, int? pageSize, CancellationToken ct)
        {
            try
            {
                var userId = ResolveUserId();
                if (!await _dashboardService.IsActiveCommitteeMemberAsync(userId, ct))
                {
                    return Forbid();
                }

                var (p, ps) = PagedRequest.Normalize(page, pageSize);
                var dto = await _dashboardService.GetSnapshotAsync(userId, ct);
                var isHead = (await _delegations.GetCommitteeIdForHeadAsync(userId, ct)).HasValue;
                var vm = new CommitteeDashboardVm
                {
                    UnderStudy = dto.UnderStudy,
                    UnderVoting = dto.UnderVoting,
                    Accepted = dto.Accepted,
                    Rejected = dto.Rejected,
                    CommitteeName = ResolveCommitteeName(),
                    IsHead = isHead,
                    Page = p,
                    PageSize = ps
                };

                var referrals = await _dashboardService.GetReferralsAsync(userId, p, ps, ct);
                if (referrals is not null)
                {
                    vm.Items = referrals.Items.Select(i => new CommitteeReferralRowVm(
                        i.IdeaId, i.Reference, i.Title, i.TitleDisplay,
                        i.StatusCode, i.StatusName, i.StatusColor,
                        i.ApplicantName, i.ApplicantDepartmentName,
                        i.ReferredAt, i.StayDays, i.IsOverdue,
                        i.DepartmentPercent, i.CommitteePercent,
                        i.MyCommitteePercent, i.HasAddedCommitteeAssessment, i.HasVoted,
                        i.DecisionNote)).ToList();
                    vm.TotalCount = referrals.TotalCount;
                }
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Committee index fallback: {Message}", ex.Message);
                ViewBag.DatabaseError = "تعذر الاتصال بقاعدة البيانات. حاول لاحقاً.";
                return View(new CommitteeDashboardVm());
            }
        }

        [HttpGet("CommitteeForMembers/Assess/{id:guid}")]
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
                Idea = dto.Idea is null ? null : ToIdeaReadOnlyVm(dto.Idea),
                Criteria = dto.Criteria.Select(c => new CommitteeCriterionVm(c.Id, c.Code, c.Name, c.Description, c.DisplayOrder)).ToList(),
                Lines = dto.Lines.Select(l => new CommitteeAssessLineVm(l.CriterionId, l.CriterionCode, l.CriterionName, l.Score, l.Comment)).ToList()
            };
            return View(vm);
        }

        [HttpGet("CommitteeForMembers/Votes")]
        public async Task<IActionResult> Votes(CancellationToken ct)
        {
            var userId = ResolveUserId();
            if ((await _delegations.GetCommitteeIdForHeadAsync(userId, ct)).HasValue)
            {
                return RedirectToAction(nameof(Index));
            }

            var dto = await _dashboardService.GetVotesAsync(userId, ct);
            if (dto is null) return Forbid();

            var vm = new CommitteeVotesVm
            {
                IsHead = false,
                Items = dto.Items.Select(i => new CommitteeVoteRowVm(
                    i.IdeaId, i.Reference, i.Title,
                    i.StatusCode, i.StatusName, i.StatusColor,
                    i.HasVoted, i.MyVote,
                    i.Description, i.ProblemStatement, i.ProposedSolution, i.ExpectedBenefits,
                    ToIdeaReadOnlyVm(i.Idea))).ToList()
            };
            return View(vm);
        }

        [HttpGet("CommitteeForMembers/Vote/{id:guid}")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Vote(Guid id, CancellationToken ct)
        {
            var userId = ResolveUserId();
            if ((await _delegations.GetCommitteeIdForHeadAsync(userId, ct)).HasValue)
            {
                return RedirectToAction(nameof(Index));
            }

            var dto = await _dashboardService.GetSingleVoteAsync(userId, id, ct);
            if (dto is null) return Forbid();

            var vm = new CommitteeVoteRowVm(
                dto.IdeaId, dto.Reference, dto.Title,
                dto.StatusCode, dto.StatusName, dto.StatusColor,
                dto.HasVoted, dto.MyVote,
                dto.Description, dto.ProblemStatement, dto.ProposedSolution, dto.ExpectedBenefits,
                ToIdeaReadOnlyVm(dto.Idea));
            return View(vm);
        }

        [HttpPost("CommitteeForMembers/Votes")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitVote(Guid ideaId, string decision, string? returnUrl, CancellationToken ct)
        {
            var userId = ResolveUserId();
            if ((await _delegations.GetCommitteeIdForHeadAsync(userId, ct)).HasValue)
            {
                return RedirectToAction(nameof(Index));
            }

            var submission = new CommitteeVoteSubmitDto(ideaId, decision);
            var result = await _dashboardService.SubmitVoteAsync(userId, submission, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message ?? "حدث خطأ.";
            TempData["AlertType"] = result.Success ? "success" : "danger";

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Votes));
        }

        [HttpPost("CommitteeForMembers/Accept")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(Guid ideaId, bool extraConfirmed, CancellationToken ct)
        {
            var userId = ResolveUserId();
            if (!(await _delegations.GetCommitteeIdForHeadAsync(userId, ct)).HasValue)
            {
                TempData["AlertError"] = "قرار اللجنة النهائي متاح لرئيس اللجنة فقط.";
                return RedirectToAction("Index", "CommitteeForMembers");
            }

            var result = await _dashboardService.AcceptAsync(userId, ideaId, extraConfirmed, ct);

            if (!result.Success)
            {
                TempData["AlertError"] = result.Message;
                TempData["AlertType"] = "danger";
                return RedirectToAction("Index", "CommitteeForMembers");
            }

            TempData["AlertMessage"] = result.Message;
            TempData["AlertType"] = "success";
            return RedirectToAction("Index", "CommitteeForMembers");
        }

        [HttpPost("CommitteeForMembers/Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(CommitteeRejectVm vm, CancellationToken ct)
        {
            var userId = ResolveUserId();
            if (!(await _delegations.GetCommitteeIdForHeadAsync(userId, ct)).HasValue)
            {
                TempData["AlertError"] = "قرار اللجنة النهائي متاح لرئيس اللجنة فقط.";
                return RedirectToAction("Index", "CommitteeForMembers");
            }

            if (!ModelState.IsValid)
            {
                TempData["AlertError"] = "سبب الرفض يجب ألا يقل عن 10 أحرف.";
                return RedirectToAction("Index", "CommitteeForMembers");
            }

            var result = await _dashboardService.RejectAsync(userId, vm.IdeaId, vm.Reason ?? string.Empty, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message ?? "حدث خطأ.";
            TempData["AlertType"] = result.Success ? "success" : "danger";

            return RedirectToAction("Index", "CommitteeForMembers");
        }

        [HttpPost("CommitteeForMembers/ReturnForDevelopment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnForDevelopment(Guid ideaId, CancellationToken ct)
        {
            var userId = ResolveUserId();
            if (!(await _delegations.GetCommitteeIdForHeadAsync(userId, ct)).HasValue)
            {
                TempData["AlertError"] = "قرار اللجنة النهائي متاح لرئيس اللجنة فقط.";
                return RedirectToAction("Index", "CommitteeForMembers");
            }

            var result = await _dashboardService.ReturnForDevelopmentAsync(userId, ideaId, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message ?? "حدث خطأ.";
            TempData["AlertType"] = result.Success ? "success" : "danger";

            return RedirectToAction("Index", "CommitteeForMembers");
        }

        [HttpGet("CommitteeForMembers/Delegations")]
        public async Task<IActionResult> Delegations(CancellationToken ct)
        {
            var vm = await BuildDelegationsVmAsync(ResolveUserId(), ct);
            if (vm is null) return Forbid();
            return View(vm);
        }

        [HttpPost("CommitteeForMembers/Delegations")]
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

        [HttpPost("CommitteeForMembers/Delegations/Cancel")]
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

        private static IdeaReadOnlyVm ToIdeaReadOnlyVm(CommitteeIdeaReadOnlyDto d)
            => new(
                d.Title,
                d.Description,
                d.ProblemStatement,
                d.ProposedSolution,
                d.ExpectedBenefits,
                d.RequiredResources,
                d.DomainName,
                d.ExpectedImpactName,
                d.ExpectedImpactOther,
                d.TargetAudienceName,
                d.TargetAudienceOther,
                d.UsesEmergingTech,
                d.TechnologyOther,
                d.CreatedAt,
                d.SubmittedAt,
                d.Attachments.Select(a => new MyRequestAttachmentVm(a.Id, a.FileName, a.SizeBytes, a.UploadedAt)).ToList());

        private Guid ResolveUserId()
        {
            var raw = User.FindFirst(RoleCodes.UserIdClaim)?.Value;
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }

        [HttpPost("CommitteeForMembers/Assess")]
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
                return RedirectToAction("Index", "CommitteeForMembers");

            return RedirectToAction(nameof(Assess), new { id = ideaId });
        }

        private string? ResolveCommitteeName()
            => User.FindFirst("ibtikar_full_name")?.Value;
    }
}
