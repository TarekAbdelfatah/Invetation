using System.Security.Claims;
using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services;
using Ibtikar.Services.Ideas;
using Ibtikar.Services.Notifications;
using Ibtikar.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Controllers
{
    [Authorize(Roles = RoleCodes.AuditEmployee)]
    public class AuditController : Controller
    {
        private readonly IbtikarDbContext _db;
        private readonly AuditLogService _auditLog;
        private readonly INotificationClient _notifier;

        public AuditController(IbtikarDbContext db, AuditLogService auditLog, INotificationClient notifier)
        {
            _db = db;
            _auditLog = auditLog;
            _notifier = notifier;
        }

        private Guid? CurrentUserId =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        private async Task SafeNotifyAsync(string action, string entityId, IDictionary<string, string>? payload, CancellationToken ct)
        {
            try
            {
                await _notifier.SendAsync(action, entityId, payload, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // request cancelled; audit action is already committed and must not roll back.
            }
        }

        private async Task<Guid?> StatusIdAsync(string code, CancellationToken ct) =>
            await _db.IdeaStatuses.Where(s => s.Code == code).Select(s => (Guid?)s.Id).FirstOrDefaultAsync(ct);

        [HttpGet]
        public async Task<IActionResult> Inbox(string? applicantType, string? status, CancellationToken ct)
        {
            var applicantTypeNorm = (applicantType ?? string.Empty).Trim().ToLowerInvariant();
            var statusNorm = (status ?? string.Empty).Trim().ToLowerInvariant();
            var items = await FetchInboxRowsAsync(applicantTypeNorm, statusNorm, ct);
            return View(new AuditInboxVm(items, applicantTypeNorm, statusNorm));
        }

        [HttpGet]
        public async Task<IActionResult> InboxRows(string? applicantType, string? status, CancellationToken ct)
        {
            var applicantTypeNorm = (applicantType ?? string.Empty).Trim().ToLowerInvariant();
            var statusNorm = (status ?? string.Empty).Trim().ToLowerInvariant();
            var items = await FetchInboxRowsAsync(applicantTypeNorm, statusNorm, ct);
            return PartialView("_InboxRowsPartial", items);
        }

        private async Task<List<AuditInboxVm.Row>> FetchInboxRowsAsync(string applicantTypeNorm, string statusNorm, CancellationToken ct)
        {
            var statusCodes = statusNorm switch
            {
                "new" => new[] { IdeaStatusCodes.New },
                "resubmitted" => new[] { IdeaStatusCodes.Resubmitted },
                "rejected" => new[] { IdeaStatusCodes.Rejected },
                _ => new[] { IdeaStatusCodes.New, IdeaStatusCodes.Resubmitted }
            };

            var ideasQuery = _db.InnovationIdeas
                .AsNoTracking()
                .Where(i => _db.IdeaStatuses
                    .Where(s => statusCodes.Contains(s.Code))
                    .Select(s => s.Id)
                    .Contains(i.CurrentStatusId));

            ideasQuery = applicantTypeNorm switch
            {
                "internal" => ideasQuery.Where(i => i.ApplicantDepartmentId != null),
                "external" => ideasQuery.Where(i => i.ApplicantDepartmentId == null),
                _ => ideasQuery
            };

            var now = DateTime.UtcNow;
            var overdueThreshold = TimeSpan.FromHours(48);

            return await ideasQuery
                .Include(i => i.CurrentStatus)
                .Include(i => i.InnovationDomain)
                .Include(i => i.ApplicantDepartment)
                .Include(i => i.ApplicantUser)
                .OrderByDescending(i => i.CreatedAt)
                .Take(50)
                .Select(i => new AuditInboxVm.Row(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.InnovationDomain != null ? i.InnovationDomain.Name : "—",
                    i.ApplicantUser != null ? i.ApplicantUser.FullName : "—",
                    i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : "—",
                    i.CreatedAt,
                    now - i.CreatedAt > overdueThreshold))
                .ToListAsync(ct);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id, CancellationToken ct)
        {
            var idea = await _db.InnovationIdeas
                .AsNoTracking()
                .Include(i => i.CurrentStatus)
                .Include(i => i.InnovationDomain)
                .Include(i => i.ExpectedImpact)
                .Include(i => i.TargetAudience)
                .Include(i => i.ApplicantUser)
                .Include(i => i.ApplicantDepartment)
                .Include(i => i.AssignedDepartment)
                .Include(i => i.StatusHistory.OrderByDescending(h => h.ChangedAt).Take(10)).ThenInclude(h => h.ToStatus)
                .Include(i => i.StatusHistory.OrderByDescending(h => h.ChangedAt).Take(10)).ThenInclude(h => h.ChangedBy)
                .Include(i => i.AuditActions.OrderByDescending(a => a.AuditDate)).ThenInclude(a => a.TargetDepartment)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (idea == null) return NotFound();

            var openStatuses = new[] { IdeaStatusCodes.New, IdeaStatusCodes.Resubmitted };
            var activeDepartments = await _db.Departments
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .Select(d => new AuditDetailsVm.DepartmentOption(d.Id, d.Name))
                .ToListAsync(ct);

            return View(new AuditDetailsVm(idea, openStatuses.Contains(idea.CurrentStatus?.Code ?? ""), activeDepartments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Open(Guid id, CancellationToken ct)
        {
            var idea = await _db.InnovationIdeas.Include(i => i.CurrentStatus).FirstOrDefaultAsync(i => i.Id == id, ct);
            if (idea == null) return NotFound();

            var openable = idea.CurrentStatus?.Code == IdeaStatusCodes.New || idea.CurrentStatus?.Code == IdeaStatusCodes.Resubmitted;
            if (!openable)
            {
                TempData["AlertMessage"] = "لا يمكن فتح هذا الملف في حالته الحالية.";
                TempData["AlertType"] = "danger";
                return RedirectToAction(nameof(Details), new { id });
            }

            var underStudyId = await StatusIdAsync(IdeaStatusCodes.UnderStudy, ct);
            if (underStudyId == null)
            {
                TempData["AlertMessage"] = "لم يتم إعداد حالة (قيد الدراسة) بعد.";
                TempData["AlertType"] = "danger";
                return RedirectToAction(nameof(Details), new { id });
            }

            var fromId = idea.CurrentStatusId;
            idea.CurrentStatusId = underStudyId.Value;
            if (idea.AuditEmployeeId is null)
            {
                idea.AuditEmployeeId = CurrentUserId;
                idea.AuditAssignedAt = DateTime.UtcNow;
            }

            _db.IdeaStatusHistories.Add(new IdeaStatusHistory
            {
                InnovationIdeaId = idea.Id,
                FromStatusId = fromId,
                ToStatusId = underStudyId.Value,
                ChangedByUserId = CurrentUserId,
                Note = "فتح الملف للدراسة"
            });

            await _db.SaveChangesAsync(ct);
            await _auditLog.WriteAsync("Audit.Open", "InnovationIdea", idea.Id.ToString(), $"Status={IdeaStatusCodes.UnderStudy}", $"Status={fromId}", ct);

            TempData["AlertMessage"] = "تم فتح الملف وبدء دراسته.";
            TempData["AlertType"] = "success";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Route(Guid id, Guid departmentId, string? decisionText, CancellationToken ct)
        {
            var idea = await _db.InnovationIdeas.Include(i => i.CurrentStatus).FirstOrDefaultAsync(i => i.Id == id, ct);
            if (idea == null) return NotFound();

            var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == departmentId && d.IsActive, ct);
            if (department == null)
            {
                TempData["AlertMessage"] = "يرجى اختيار إدارة تحويل صحيحة.";
                TempData["AlertType"] = "danger";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (idea.CurrentStatus?.Code != IdeaStatusCodes.UnderStudy)
            {
                TempData["AlertMessage"] = "لا يمكن التحويل لجهة إلا بعد فتح الملف للدراسة.";
                TempData["AlertType"] = "danger";
                return RedirectToAction(nameof(Details), new { id });
            }

            idea.AssignedDepartmentId = departmentId;
            idea.AuditEmployeeId = CurrentUserId;

            _db.AuditActionItems.Add(new AuditActionItem
            {
                IdeaId = idea.Id,
                Decision = "route",
                DecisionText = decisionText,
                TargetDepartmentId = departmentId,
                AuditorId = CurrentUserId ?? Guid.Empty
            });

            await _db.SaveChangesAsync(ct);
            await _auditLog.WriteAsync("Audit.Route", "InnovationIdea", idea.Id.ToString(),
                $"AssignedDepartmentId={departmentId}", $"Status={idea.CurrentStatus?.Code}", ct);
            await SafeNotifyAsync("Audit.Route", idea.Id.ToString(), new Dictionary<string, string>
            {
                ["departmentId"] = departmentId.ToString(),
                ["departmentName"] = department.Name
            }, ct);

            TempData["AlertMessage"] = $"تم تحويل الملف إلى إدارة: {department.Name}.";
            TempData["AlertType"] = "success";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string? reason, CancellationToken ct)
        {
            var idea = await _db.InnovationIdeas.Include(i => i.CurrentStatus).FirstOrDefaultAsync(i => i.Id == id, ct);
            if (idea == null) return NotFound();

            if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
            {
                TempData["AlertMessage"] = "يرجى إدخال سبب رفض لا يقل عن 10 أحرف.";
                TempData["AlertType"] = "danger";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (idea.CurrentStatus?.Code == IdeaStatusCodes.Rejected || idea.CurrentStatus?.IsTerminal == true)
            {
                TempData["AlertMessage"] = "الملف في حالة نهائية ولا يمكن رفضه مجدداً.";
                TempData["AlertType"] = "danger";
                return RedirectToAction(nameof(Details), new { id });
            }

            var rejectedId = await StatusIdAsync(IdeaStatusCodes.Rejected, ct);
            if (rejectedId == null)
            {
                TempData["AlertMessage"] = "لم يتم إعداد حالة (مرفوض) بعد.";
                TempData["AlertType"] = "danger";
                return RedirectToAction(nameof(Details), new { id });
            }

            var fromId = idea.CurrentStatusId;
            idea.CurrentStatusId = rejectedId.Value;

            _db.AuditActionItems.Add(new AuditActionItem
            {
                IdeaId = idea.Id,
                Decision = "reject",
                DecisionText = reason.Trim(),
                AuditorId = CurrentUserId ?? Guid.Empty
            });

            _db.IdeaStatusHistories.Add(new IdeaStatusHistory
            {
                InnovationIdeaId = idea.Id,
                FromStatusId = fromId,
                ToStatusId = rejectedId.Value,
                ChangedByUserId = CurrentUserId,
                Note = reason.Trim()
            });

            await _db.SaveChangesAsync(ct);
            await _auditLog.WriteAsync("Audit.Reject", "InnovationIdea", idea.Id.ToString(),
                $"Status={IdeaStatusCodes.Rejected}", $"Status={fromId}", ct);
            await SafeNotifyAsync("Audit.Reject", idea.Id.ToString(), new Dictionary<string, string>
            {
                ["reasonLength"] = reason.Trim().Length.ToString()
            }, ct);

            TempData["AlertMessage"] = "تم رفض الملف.";
            TempData["AlertType"] = "success";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCompletion(Guid id, string? instructions, CancellationToken ct)
        {
            var idea = await _db.InnovationIdeas.Include(i => i.CurrentStatus).FirstOrDefaultAsync(i => i.Id == id, ct);
            if (idea == null) return NotFound();

            if (string.IsNullOrWhiteSpace(instructions) || instructions.Length < 10)
            {
                TempData["AlertMessage"] = "يرجى إدخال تعليمات الاستكمال التي لا تقل عن 10 أحرف.";
                TempData["AlertType"] = "danger";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (idea.CurrentStatus?.IsTerminal == true)
            {
                TempData["AlertMessage"] = "الملف في حالة نهائية ولا يمكن طلب استكماله.";
                TempData["AlertType"] = "danger";
                return RedirectToAction(nameof(Details), new { id });
            }

            var waitingId = await StatusIdAsync(IdeaStatusCodes.WaitingForCompletion, ct);
            if (waitingId == null)
            {
                TempData["AlertMessage"] = "لم يتم إعداد حالة (بانتظار الاستكمال) بعد.";
                TempData["AlertType"] = "danger";
                return RedirectToAction(nameof(Details), new { id });
            }

            var fromId = idea.CurrentStatusId;
            idea.CurrentStatusId = waitingId.Value;

            _db.AuditActionItems.Add(new AuditActionItem
            {
                IdeaId = idea.Id,
                Decision = "request_completion",
                DecisionText = instructions.Trim(),
                AuditorId = CurrentUserId ?? Guid.Empty
            });

            _db.IdeaStatusHistories.Add(new IdeaStatusHistory
            {
                InnovationIdeaId = idea.Id,
                FromStatusId = fromId,
                ToStatusId = waitingId.Value,
                ChangedByUserId = CurrentUserId,
                Note = instructions.Trim()
            });

            await _db.SaveChangesAsync(ct);
            await _auditLog.WriteAsync("Audit.RequestCompletion", "InnovationIdea", idea.Id.ToString(),
                $"Status={IdeaStatusCodes.WaitingForCompletion}", $"Status={fromId}", ct);
            await SafeNotifyAsync("Audit.RequestCompletion", idea.Id.ToString(), null, ct);

            TempData["AlertMessage"] = "تم طلب استكمال الملف من مقدمه.";
            TempData["AlertType"] = "success";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    public class AuditInboxVm
    {
        public List<Row> Items { get; }
        public string ApplicantType { get; }
        public string Status { get; }
        public AuditInboxVm(List<Row> items, string applicantType, string status)
        {
            Items = items;
            ApplicantType = applicantType ?? string.Empty;
            Status = status ?? string.Empty;
        }
        public record Row(Guid Id, string Reference, string Title, string Domain, string ApplicantName, string Department, DateTime SubmittedAt, bool IsOverdue);
    }

    public class AuditDetailsVm
    {
        public Guid Id { get; }
        public string Reference { get; }
        public string Title { get; }
        public string Description { get; }
        public string? ProblemStatement { get; }
        public string? ProposedSolution { get; }
        public string? ExpectedBenefits { get; }
        public string Domain { get; }
        public string ApplicantName { get; }
        public string ApplicantDepartment { get; }
        public string? AssignedDepartment { get; }
        public string StatusName { get; }
        public string StatusColor { get; }
        public DateTime SubmittedAt { get; }
        public bool CanOpen { get; }
        public bool IsUnderStudy { get; }
        public bool IsTerminal { get; }
        public List<DepartmentOption> ActiveDepartments { get; }
        public List<AuditHistoryRow> History { get; }

        public AuditDetailsVm(InnovationIdea i, bool canOpen, List<DepartmentOption> activeDepartments)
        {
            Id = i.Id;
            Reference = i.ReferenceNumber;
            Title = i.Title;
            Description = i.Description;
            ProblemStatement = i.ProblemStatement;
            ProposedSolution = i.ProposedSolution;
            ExpectedBenefits = i.ExpectedBenefits;
            Domain = i.InnovationDomain?.Name ?? "—";
            ApplicantName = i.ApplicantUser?.FullName ?? "—";
            ApplicantDepartment = i.ApplicantDepartment?.Name ?? "خارجي";
            AssignedDepartment = i.AssignedDepartment?.Name;
            StatusName = i.CurrentStatus?.Name ?? "—";
            StatusColor = i.CurrentStatus?.Color ?? "#6c757d";
            SubmittedAt = i.SubmittedAt ?? i.CreatedAt;
            CanOpen = canOpen;
            IsUnderStudy = i.CurrentStatus?.Code == IdeaStatusCodes.UnderStudy;
            IsTerminal = i.CurrentStatus?.IsTerminal == true;
            ActiveDepartments = activeDepartments;
            History = i.StatusHistory.Select(h => new AuditHistoryRow(
                h.ChangedAt,
                h.FromStatus?.NameEn ?? "—",
                h.ToStatus?.Name ?? "—",
                h.ChangedBy?.FullName ?? "—",
                h.Note)).ToList();
        }

        public record DepartmentOption(Guid Id, string Name);
        public record AuditHistoryRow(DateTime ChangedAt, string FromStatus, string ToStatus, string By, string? Note);
    }
}
