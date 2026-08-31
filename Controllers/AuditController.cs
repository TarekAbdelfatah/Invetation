using Ibtikar.DTOs.Audit;
using Ibtikar.Services.Audit;
using Ibtikar.Services.Security;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.AuditEmployee)]
    public class AuditController : Controller
    {
        private readonly IAuditService _service;

        public AuditController(IAuditService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Inbox(string? applicantType, string? status, CancellationToken ct)
        {
            var dto = await _service.GetInboxAsync(applicantType, status, ct);
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
            => new(dto.Items.Select(ToRowVm).ToList(), dto.ApplicantType, dto.Status);

        private static AuditInboxVm.Row ToRowVm(AuditInboxRowDto dto)
            => new(dto.Id, dto.Reference, dto.Title, dto.Domain, dto.ApplicantName, dto.Department, dto.SubmittedAt, dto.IsOverdue);

        private static AuditDetailsVm ToDetailsVm(AuditDetailsDto dto)
            => new(
                dto.Id,
                dto.Reference,
                dto.Title,
                dto.Description,
                dto.ProblemStatement,
                dto.ProposedSolution,
                dto.ExpectedBenefits,
                dto.Domain,
                dto.ApplicantName,
                dto.ApplicantDepartment,
                dto.AssignedDepartment,
                dto.StatusName,
                dto.StatusColor,
                dto.SubmittedAt,
                dto.CanOpen,
                dto.IsUnderStudy,
                dto.IsTerminal,
                dto.ActiveDepartments.Select(d => new AuditDetailsVm.DepartmentOption(d.Id, d.Name)).ToList(),
                dto.History.Select(h => new AuditDetailsVm.AuditHistoryRow(h.ChangedAt, h.FromStatus, h.ToStatus, h.By, h.Note)).ToList());
    }
}