using Ibtikar.DTOs.MyRequests;
using Ibtikar.Services.Interfaces;
using Ibtikar.Services.Helpers;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.ExternalBeneficiary + "," + RoleCodes.InternalBeneficiary)]
    public class MyRequestsController : Controller
    {
        private readonly IMyRequestsService _service;

        public MyRequestsController(IMyRequestsService service) => _service = service;

        public async Task<IActionResult> Index(int? page, int? pageSize, CancellationToken ct)
        {
            var applicantId = ResolveApplicantId();
            if (applicantId == Guid.Empty) return Challenge();

            var (p, ps) = PagedRequest.Normalize(page, pageSize);
            var dto = await _service.GetListAsync(applicantId, p, ps, ct);
            return View(ToListVm(dto));
        }

        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var applicantId = ResolveApplicantId();
            if (applicantId == Guid.Empty) return Challenge();

            var dto = await _service.GetDetailsAsync(applicantId, id, ct);
            if (dto is null) return NotFound();

            return View(ToDetailsVm(dto));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var applicantId = ResolveApplicantId();
            if (applicantId == Guid.Empty) return Challenge();

            var result = await _service.DeleteAsync(applicantId, id, ct);
            return result.Status switch
            {
                MyRequestDeleteStatus.NotFound => NotFound(),
                MyRequestDeleteStatus.NotDeletable => BadRequest(result.Message),
                _ => RedirectToAction(nameof(Index))
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResubmitCompletion(
            Guid id,
            MyRequestResubmitVm vm,
            CancellationToken ct)
        {
            return await ResubmitInternalAsync(
                id,
                vm,
                (svc, aid, ideaId, content, c) => svc.ResubmitCompletionAsync(aid, ideaId, content, c),
                ct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResubmitDeveloped(
            Guid id,
            MyRequestResubmitVm vm,
            CancellationToken ct)
        {
            return await ResubmitInternalAsync(
                id,
                vm,
                (svc, aid, ideaId, content, c) => svc.ResubmitDevelopedAsync(aid, ideaId, content, c),
                ct);
        }

        private async Task<IActionResult> ResubmitInternalAsync(
            Guid id,
            MyRequestResubmitVm vm,
            Func<IMyRequestsService, Guid, Guid, MyRequestContentUpdateDto, CancellationToken, Task<MyRequestResubmitResult>> invoke,
            CancellationToken ct)
        {
            var applicantId = ResolveApplicantId();
            if (applicantId == Guid.Empty) return Challenge();

            if (!ModelState.IsValid)
            {
                var dto = await _service.GetDetailsAsync(applicantId, id, ct);
                if (dto is null) return NotFound();
                return View(nameof(Details), ToDetailsVm(dto));
            }

            var content = new MyRequestContentUpdateDto(
                vm.Description ?? string.Empty,
                vm.ProblemStatement,
                vm.ProposedSolution,
                vm.ExpectedBenefits);
            var result = await invoke(_service, applicantId, id, content, ct);

            return result.Status switch
            {
                MyRequestResubmitStatus.NotFound => NotFound(),
                MyRequestResubmitStatus.WrongStatus => BadRequest(result.Message),
                MyRequestResubmitStatus.EmptyDescription => BadRequest(result.Message),
                MyRequestResubmitStatus.NoMaterialChange => UnprocessableEntity(new { error = result.Message }),
                _ => RedirectToAction(nameof(Details), new { id })
            };
        }

        private Guid ResolveApplicantId()
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
        }

        private static MyRequestsVm ToListVm(MyRequestsListDto dto)
            => new(dto.Items.Select(ToItemVm).ToList(), dto.Page, dto.PageSize, dto.TotalCount);

        private static MyRequestVm ToItemVm(MyRequestSummaryDto dto)
            => new(
                dto.Id,
                dto.Reference,
                dto.Title,
                dto.IsDraft,
                dto.StatusCode,
                dto.StatusName,
                dto.StatusColor,
                dto.CreatedAt,
                dto.SubmittedAt);

        private static MyRequestDetailsVm ToDetailsVm(MyRequestDetailsDto dto)
            => new(
                dto.Id,
                dto.Reference,
                dto.Title,
                dto.Description,
                dto.ProblemStatement,
                dto.ProposedSolution,
                dto.ExpectedBenefits,
                dto.ExpectedImpactOther,
                dto.TargetAudienceOther,
                dto.UsesEmergingTech,
                dto.TechnologyOther,
                dto.StatusCode,
                dto.StatusName,
                dto.StatusColor,
                dto.DomainName,
                dto.ExpectedImpactName,
                dto.TargetAudienceName,
                dto.CreatedAt,
                dto.SubmittedAt,
                dto.CompletionNotes,
                dto.DevelopmentNotes,
                dto.RejectionReason,
                dto.Attachments.Select(a => new MyRequestAttachmentVm(a.Id, a.FileName, a.SizeBytes, a.UploadedAt)).ToList());
    }
}