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

        private Guid ResolveUserId()
        {
            var raw = User.FindFirst(RoleCodes.UserIdClaim)?.Value;
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }

        private string? ResolveCommitteeName()
            => User.FindFirst("ibtikar_full_name")?.Value;
    }
}
