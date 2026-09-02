using Ibtikar.DTOs.Audit;
using Ibtikar.Services.Interfaces;
using Ibtikar.Services.Helpers;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [IbtikarAuthorize(RoleCodes.AuditEmployee)]
    public class AuditController : Controller
    {
        private readonly IAuditService _service;

        public AuditController(IAuditService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Inbox(string? applicantType, string? status, int? page, int? pageSize, CancellationToken ct)
        {
            var (p, ps) = PagedRequest.Normalize(page, pageSize);
            var dto = await _service.GetInboxAsync(applicantType, status, p, ps, ct);
            return View(ToInboxVm(dto));
        }

        [HttpGet]
        public async Task<IActionResult> InboxRows(string? applicantType, string? status, CancellationToken ct)
        {
            var rows = await _service.GetInboxRowsAsync(applicantType, status, ct);
            return PartialView("_InboxRowsPartial", rows.Select(ToRowVm).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var dto = await _service.GetDetailsAsync(id, ct);
            if (dto is null) return NotFound();
            return View(ToDetailsVm(dto));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Open(Guid id, CancellationToken ct)
        {
            var result = await _service.OpenAsync(id, CurrentUserId, ct);
            ApplyResult(result);
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Route(Guid id, Guid departmentId, string? decisionText, CancellationToken ct)
        {
            var result = await _service.RouteAsync(id, departmentId, decisionText, CurrentUserId, ct);
            ApplyResult(result);
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string? reason, CancellationToken ct)
        {
            var result = await _service.RejectAsync(id, reason, CurrentUserId, ct);
            ApplyResult(result);
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCompletion(Guid id, string? instructions, CancellationToken ct)
        {
            var result = await _service.RequestCompletionAsync(id, instructions, CurrentUserId, ct);
            ApplyResult(result);
            return RedirectToAction(nameof(Details), new { id });
        }

        private Guid? CurrentUserId =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        private void ApplyResult(AuditActionResultDto result)
        {
            switch (result.Outcome)
            {
                case AuditActionOutcome.Success:
                    TempData["AlertMessage"] = result.Message;
                    TempData["AlertType"] = "success";
                    break;
                case AuditActionOutcome.NotFound:
                    break;
                default:
                    TempData["AlertMessage"] = result.Message;
                    TempData["AlertType"] = "danger";
                    break;
            }
        }

        private static AuditInboxVm ToInboxVm(AuditInboxDto dto)
            => new(dto.Items.Select(ToRowVm).ToList(), dto.ApplicantType, dto.Status, dto.Page, dto.PageSize, dto.TotalCount);

        private static AuditInboxVm.Row ToRowVm(AuditInboxRowDto dto)
            => new(dto.Id, dto.Reference, dto.Title, dto.Domain, dto.ApplicantName, dto.Department, dto.AssignedDepartment, dto.StatusCode, dto.StatusName, dto.StatusColor, dto.SubmittedAt, dto.IsOverdue);

        private static AuditDetailsVm ToDetailsVm(AuditDetailsDto dto)
            => new(
                dto.Id,
                dto.Reference,
                dto.Title,
                dto.Description,
                dto.ProblemStatement,
                dto.ProposedSolution,
                dto.ExpectedBenefits,
                dto.RequiredResources,
                dto.ExpectedImpactName,
                dto.ExpectedImpactOther,
                dto.TargetAudienceName,
                dto.TargetAudienceOther,
                dto.UsesEmergingTech,
                dto.TechnologyOther,
                dto.Domain,
                dto.ApplicantName,
                dto.ApplicantDepartment,
                dto.AssignedDepartment,
                dto.StatusCode,
                dto.StatusName,
                dto.StatusColor,
                dto.SubmittedAt,
                dto.CanDecide,
                dto.IsUnderStudy,
                dto.IsRoutedToSpecialist,
                dto.IsTerminal,
                dto.LatestCompletionNote,
                dto.LatestCompletionNoteAt,
                dto.ReturnedBySpecialistReason,
                dto.ReturnedBySpecialistDepartment,
                dto.ReturnedBySpecialistAt,
                dto.ActiveDepartments.Select(d => new AuditDetailsVm.DepartmentOption(d.Id, d.Name)).ToList(),
                dto.History.Select(h => new AuditDetailsVm.AuditHistoryRow(h.ChangedAt, h.FromStatus, h.ToStatus, h.By, h.Note)).ToList(),
                dto.Attachments.Select(a => new AuditDetailsVm.Attachment(a.Id, a.FileName, a.SizeBytes, a.ContentType, a.UploadedAt)).ToList());
    }
}