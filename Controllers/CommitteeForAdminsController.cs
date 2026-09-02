using Ibtikar.DTOs.Committees;
using Ibtikar.Services.Interfaces;
using Ibtikar.Services.Helpers;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.SystemAdmin)]
    public class CommitteeForAdminsController : Controller
    {
        private readonly ICommitteeFormationService _service;
        private readonly ILogger<CommitteeForAdminsController> _logger;

        public CommitteeForAdminsController(ICommitteeFormationService service, ILogger<CommitteeForAdminsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet("CommitteeForAdmins")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            try
            {
                var dtos = await _service.GetAllAsync(ct);
                var vm = new CommitteesIndexVm
                {
                    Committees = dtos.Select(ToSummaryVm).ToList()
                };
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Committees index fallback: {Message}", ex.Message);
                ViewBag.DatabaseError = "تعذر الاتصال بقاعدة البيانات. حاول لاحقاً.";
                return View(new CommitteesIndexVm());
            }
        }

        [HttpGet("CommitteeForAdmins/Create")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var candidates = await _service.GetMemberCandidatesAsync(null, ct);
            var vm = new CommitteesCreateVm
            {
                MemberCandidates = candidates.Select(c => new CommitteeMemberOptionVm
                {
                    UserId = c.UserId,
                    FullName = c.FullName,
                    Username = c.Username
                }).ToList(),
                HeadCandidates = candidates.Select(c => new CommitteeMemberOptionVm
                {
                    UserId = c.UserId,
                    FullName = c.FullName,
                    Username = c.Username
                }).ToList()
            };
            return View(vm);
        }

        [HttpPost("CommitteeForAdmins/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommitteesCreateVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await ReloadCandidatesAsync(vm, ct);
                return View(vm);
            }

            var dto = new CommitteeCreateDto(
                vm.Name,
                vm.Description,
                vm.HeadUserId ?? Guid.Empty,
                vm.MemberUserIds ?? new List<Guid>());

            var result = await _service.CreateAsync(ResolveUserId(), dto, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message;
            TempData["AlertType"] = result.Success ? "success" : "danger";

            if (!result.Success)
            {
                await ReloadCandidatesAsync(vm, ct);
                return View(vm);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("CommitteeForAdmins/Activate/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
        {
            var result = await _service.ActivateAsync(ResolveUserId(), id, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message;
            TempData["AlertType"] = result.Success ? "success" : "danger";

            return RedirectToAction(nameof(Index));
        }

        private async Task ReloadCandidatesAsync(CommitteesCreateVm vm, CancellationToken ct)
        {
            var candidates = await _service.GetMemberCandidatesAsync(null, ct);
            var list = candidates.Select(c => new CommitteeMemberOptionVm
            {
                UserId = c.UserId,
                FullName = c.FullName,
                Username = c.Username
            }).ToList();
            vm.MemberCandidates = list;
            vm.HeadCandidates = list;
        }

        private static CommitteeSummaryVm ToSummaryVm(CommitteeSummaryDto dto)
            => new()
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt,
                ActivatedAt = dto.ActivatedAt,
                HeadUserName = dto.HeadUserName,
                MemberCount = dto.MemberCount
            };

        private Guid ResolveUserId()
        {
            var raw = User.FindFirst(RoleCodes.UserIdClaim)?.Value;
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }
    }
}
