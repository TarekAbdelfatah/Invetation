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

            return RedirectToAction(nameof(Details), new { id = result.CommitteeId });
        }

        [HttpGet("CommitteeForAdmins/Details/{id:guid}")]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var dto = await _service.GetDetailAsync(id, ct);
            if (dto is null)
            {
                TempData["AlertError"] = "اللجنة غير موجودة.";
                TempData["AlertType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            var vm = new CommitteeDetailVm
            {
                Id = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedAt = dto.CreatedAt,
                ActivatedAt = dto.ActivatedAt,
                HeadUserName = dto.HeadUserName,
                HeadUsername = dto.HeadUsername,
                Members = dto.Members.Select(m => new CommitteeMemberDetailVm
                {
                    UserId = m.UserId,
                    FullName = m.FullName,
                    Username = m.Username,
                    IsHead = m.IsHead
                }).ToList()
            };
            return View(vm);
        }

        [HttpGet("CommitteeForAdmins/Edit/{id:guid}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var detail = await _service.GetDetailAsync(id, ct);
            if (detail is null)
            {
                TempData["AlertError"] = "اللجنة غير موجودة.";
                TempData["AlertType"] = "danger";
                return RedirectToAction(nameof(Index));
            }

            var candidates = await _service.GetMemberCandidatesAsync(id, ct);
            var currentMemberIds = detail.Members.Where(m => !m.IsHead).Select(m => m.UserId).ToList();

            var allCandidates = detail.Members
                .Where(m => !m.IsHead)
                .Select(m => new CommitteeMemberOptionVm { UserId = m.UserId, FullName = m.FullName, Username = m.Username })
                .Union(candidates.Select(c => new CommitteeMemberOptionVm { UserId = c.UserId, FullName = c.FullName, Username = c.Username }))
                .GroupBy(c => c.UserId)
                .Select(g => g.First())
                .OrderBy(c => c.FullName)
                .ToList();

            var headCandidates = detail.Members
                .Select(m => new CommitteeMemberOptionVm { UserId = m.UserId, FullName = m.FullName, Username = m.Username })
                .Union(candidates.Select(c => new CommitteeMemberOptionVm { UserId = c.UserId, FullName = c.FullName, Username = c.Username }))
                .GroupBy(c => c.UserId)
                .Select(g => g.First())
                .OrderBy(c => c.FullName)
                .ToList();

            var vm = new CommitteesEditVm
            {
                Id = detail.Id,
                Name = detail.Name,
                Description = detail.Description,
                HeadUserId = detail.HeadUserId,
                MemberUserIds = currentMemberIds,
                MemberCandidates = allCandidates,
                HeadCandidates = headCandidates
            };
            return View(vm);
        }

        [HttpPost("CommitteeForAdmins/Edit/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CommitteesEditVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                await ReloadEditCandidatesAsync(vm, ct);
                return View(vm);
            }

            var dto = new CommitteeEditDto(
                vm.Id,
                vm.Name,
                vm.Description,
                vm.HeadUserId ?? Guid.Empty,
                vm.MemberUserIds ?? new List<Guid>());

            var result = await _service.UpdateAsync(ResolveUserId(), dto, ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message;
            TempData["AlertType"] = result.Success ? "success" : "danger";

            if (!result.Success)
            {
                await ReloadEditCandidatesAsync(vm, ct);
                return View(vm);
            }

            return RedirectToAction(nameof(Details), new { id = vm.Id });
        }

        private async Task ReloadEditCandidatesAsync(CommitteesEditVm vm, CancellationToken ct)
        {
            var detail = await _service.GetDetailAsync(vm.Id, ct);
            if (detail is null) return;

            var candidates = await _service.GetMemberCandidatesAsync(vm.Id, ct);
            var currentMemberIds = detail.Members.Where(m => !m.IsHead).Select(m => m.UserId).ToList();

            vm.MemberCandidates = detail.Members
                .Where(m => !m.IsHead)
                .Select(m => new CommitteeMemberOptionVm { UserId = m.UserId, FullName = m.FullName, Username = m.Username })
                .Union(candidates.Select(c => new CommitteeMemberOptionVm { UserId = c.UserId, FullName = c.FullName, Username = c.Username }))
                .GroupBy(c => c.UserId)
                .Select(g => g.First())
                .OrderBy(c => c.FullName)
                .ToList();

            vm.HeadCandidates = detail.Members
                .Select(m => new CommitteeMemberOptionVm { UserId = m.UserId, FullName = m.FullName, Username = m.Username })
                .Union(candidates.Select(c => new CommitteeMemberOptionVm { UserId = c.UserId, FullName = c.FullName, Username = c.Username }))
                .GroupBy(c => c.UserId)
                .Select(g => g.First())
                .OrderBy(c => c.FullName)
                .ToList();

            vm.MemberUserIds = detail.Members.Where(m => !m.IsHead).Select(m => m.UserId).ToList();
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
