using Ibtikar.DTOs.Execution;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Implementations;
using Ibtikar.Services.Interfaces;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [IbtikarAuthorize(RoleCodes.SpecializedDepartment)]
    public class ExecutionController : Controller
    {
        private readonly IExecutionService _service;

        public ExecutionController(IExecutionService service) => _service = service;

        [HttpGet("Execution")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var dto = await _service.GetListAsync(ResolveDepartmentId(), ct);
            var vm = new ExecutionListVm
            {
                DepartmentName = dto.DepartmentName,
                Items = dto.Items.Select(i => new ExecutionListRowVm(
                    i.IdeaId, i.Reference, i.Title, i.DomainName, i.ApplicantName,
                    i.AssignedAt, i.CurrentStageName, i.StatusName, i.StatusColor,
                    i.CanUpdate, i.CanComplete)).ToList()
            };
            return View(vm);
        }

        [HttpGet("Execution/Update/{id:guid}")]
        public async Task<IActionResult> Update(Guid id, CancellationToken ct)
        {
            var vm = await BuildHeaderVmAsync(id, ct);
            if (vm is null) return Forbid();
            return View(vm);
        }

        [HttpPost("Execution/Update/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Guid id, ExecutionStageUpdateVm vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var headerVm = await BuildHeaderVmAsync(id, ct);
                if (headerVm is null) return Forbid();
                headerVm.Note = vm.Note;
                headerVm.ExecutionStageId = vm.ExecutionStageId;
                return View(headerVm);
            }

            var dto = new ExecutionUpdateDto(id, vm.ExecutionStageId, vm.Note ?? string.Empty);
            var result = await _service.UpdateStageAsync(dto, ResolveUserId(), ResolveDepartmentId(), ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message ?? "حدث خطأ.";
            TempData["AlertType"] = result.Success ? "success" : "danger";
            return RedirectToAction(nameof(Update), new { id });
        }

        [HttpPost("Execution/Complete/{id:guid}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(Guid id, ExecutionCompleteVm vm, IFormCollection form, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var headerVm = await BuildHeaderVmAsync(id, ct);
                if (headerVm is null) return Forbid();
                headerVm.CompleteNote = vm.CompleteNote;
                headerVm.CompletionStageId = vm.CompletionStageId;
                return View(headerVm);
            }

            var ids = form["attachmentId"]
                .Where(v => Guid.TryParse(v, out _))
                .Select(v => Guid.Parse(v!))
                .Distinct()
                .ToList();
            var dto = new ExecutionCompleteDto(id, vm.CompletionStageId, vm.CompleteNote ?? string.Empty, ids);
            var result = await _service.CompleteAsync(dto, ResolveUserId(), ResolveDepartmentId(), ct);

            TempData[result.Success ? "AlertMessage" : "AlertError"] = result.Message ?? "حدث خطأ.";
            TempData["AlertType"] = result.Success ? "success" : "danger";
            return RedirectToAction(nameof(Update), new { id });
        }

        private async Task<ExecutionHeaderVm?> BuildHeaderVmAsync(Guid id, CancellationToken ct)
        {
            var dto = await _service.GetHeaderAsync(id, ResolveDepartmentId(), ct);
            if (dto is null) return null;

            return new ExecutionHeaderVm
            {
                IdeaId = dto.IdeaId,
                Reference = dto.Reference,
                Title = dto.Title,
                DomainName = dto.DomainName,
                ApplicantName = dto.ApplicantName,
                ApplicantDepartmentName = dto.ApplicantDepartmentName,
                AssignedDepartmentName = dto.AssignedDepartmentName,
                StatusName = dto.StatusName,
                StatusColor = dto.StatusColor,
                Stages = dto.Stages.Select(s => new ExecutionStageOptionVm(s.Id, s.Order, s.Code, s.Name)).ToList(),
                CurrentStageId = dto.CurrentStage?.Id,
                CurrentStageName = dto.CurrentStage?.Name,
                CurrentStageOrder = dto.CurrentStage?.Order ?? 0,
                CanUpdate = dto.CanUpdate,
                CanComplete = dto.CanComplete
            };
        }

        [HttpPost("Execution/UploadCompletion")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadCompletion(Guid ideaId, [FromForm] List<IFormFile> files, CancellationToken ct)
        {
            if (files is null || files.Count != 2)
                return BadRequest(new { success = false, message = "يجب رفع ملفَي PDF بالضبط." });

            var userId = ResolveUserId();
            if (userId is null)
                return Unauthorized(new { success = false, message = "الجلسة منتهية." });

            var attachmentService = HttpContext.RequestServices.GetRequiredService<AttachmentService>();
            var saved = new List<Guid>();
            foreach (var f in files)
            {
                var result = await attachmentService.SaveAsync(ideaId, userId.Value, f, ct);
                if (!result.Success)
                    return BadRequest(new { success = false, message = result.Error ?? "فشل حفظ الملف." });
                if (result.AttachmentId is not null) saved.Add(result.AttachmentId.Value);
            }
            if (saved.Count != 2)
                return BadRequest(new { success = false, message = "تعذر حفظ الملفين." });

            return Ok(new { success = true, attachmentIds = saved });
        }

        [HttpGet("Execution/Timeline/{id:guid}")]
        public async Task<IActionResult> Timeline(Guid id, CancellationToken ct)
        {
            var dto = await _service.GetTimelineAsync(id, ResolveDepartmentId(), ct);
            if (dto is null) return Forbid();

            var vm = new ExecutionTimelineVm
            {
                IdeaId = dto.IdeaId,
                Reference = dto.Reference,
                Title = dto.Title,
                Rows = dto.Rows.Select(r => new ExecutionTimelineRowVm(
                    r.ChangedAt, r.StageName, r.StageOrder, r.ChangedByName, r.Note)).ToList()
            };
            return View(vm);
        }

        private Guid? ResolveDepartmentId()
        {
            var raw = User.FindFirst(RoleCodes.DepartmentIdClaim)?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }

        private Guid? ResolveUserId()
        {
            var raw = User.FindFirst(RoleCodes.UserIdClaim)?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }
}
