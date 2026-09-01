using Ibtikar.DTOs.Audit;
using Ibtikar.Models;
using Ibtikar.Repositories;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Notifications;

namespace Ibtikar.Services.Audit
{
    public sealed class AuditService : IAuditService
    {
        private const int InboxTake = 50;

        private readonly IAuditRepository _repo;
        private readonly AuditLogService _auditLog;
        private readonly INotificationClient _notifier;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            IAuditRepository repo,
            AuditLogService auditLog,
            INotificationClient notifier,
            ILogger<AuditService> logger)
        {
            _repo = repo;
            _auditLog = auditLog;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<AuditInboxDto> GetInboxAsync(string? applicantType, string? status, CancellationToken ct)
        {
            var applicantTypeNorm = Normalize(applicantType);
            var statusNorm = Normalize(status);
            var rows = await GetInboxRowsInternalAsync(applicantTypeNorm, statusNorm, ct);
            return new AuditInboxDto(rows, applicantTypeNorm, statusNorm);
        }

        public async Task<IReadOnlyList<AuditInboxRowDto>> GetInboxRowsAsync(string? applicantType, string? status, CancellationToken ct)
        {
            var applicantTypeNorm = Normalize(applicantType);
            var statusNorm = Normalize(status);
            return await GetInboxRowsInternalAsync(applicantTypeNorm, statusNorm, ct);
        }

        public Task<AuditDetailsDto?> GetDetailsAsync(Guid id, CancellationToken ct)
            => _repo.GetDetailsAsync(id, ct);

        public async Task<AuditActionResultDto> OpenAsync(Guid id, Guid? auditorId, CancellationToken ct)
        {
            var idea = await _repo.GetForTransitionAsync(id, ct);
            if (idea is null) return new(AuditActionOutcome.NotFound, null);

            var currentCode = idea.CurrentStatus?.Code;
            if (currentCode != IdeaStatusCodes.New && currentCode != IdeaStatusCodes.Resubmitted)
                return new(AuditActionOutcome.InvalidState, "لا يمكن فتح هذا الملف في حالته الحالية.");

            var underStudyId = await _repo.GetStatusIdByCodeAsync(IdeaStatusCodes.UnderStudy, ct);
            if (underStudyId is null)
                return new(AuditActionOutcome.InvalidState, "لم يتم إعداد حالة (قيد الدراسة) بعد.");

            var fromId = idea.CurrentStatusId;
            idea.CurrentStatusId = underStudyId.Value;
            if (idea.AuditEmployeeId is null)
            {
                idea.AuditEmployeeId = auditorId;
                idea.AuditAssignedAt = DateTime.UtcNow;
            }

            await _repo.AddStatusHistoryAsync(new IdeaStatusHistory
            {
                InnovationIdeaId = idea.Id,
                FromStatusId = fromId,
                ToStatusId = underStudyId.Value,
                ChangedByUserId = auditorId,
                Note = "فتح الملف للدراسة"
            }, ct);

            await _repo.SaveChangesAsync(ct);
            await _auditLog.WriteAsync("Audit.Open", "InnovationIdea", idea.Id.ToString(),
                $"Status={IdeaStatusCodes.UnderStudy}", $"Status={fromId}", ct);

            return new(AuditActionOutcome.Success, "تم فتح الملف وبدء دراسته.");
        }

        public async Task<AuditActionResultDto> RouteAsync(Guid id, Guid departmentId, string? decisionText, Guid? auditorId, CancellationToken ct)
        {
            var idea = await _repo.GetForTransitionAsync(id, ct);
            if (idea is null) return new(AuditActionOutcome.NotFound, null);

            var department = await _repo.GetActiveDepartmentAsync(departmentId, ct);
            if (department is null)
                return new(AuditActionOutcome.InvalidInput, "يرجى اختيار إدارة تحويل صحيحة.");

            if (idea.CurrentStatus?.Code != IdeaStatusCodes.UnderStudy)
                return new(AuditActionOutcome.InvalidState, "لا يمكن التحويل لجهة إلا بعد فتح الملف للدراسة.");

            idea.AssignedDepartmentId = departmentId;
            idea.AuditEmployeeId = auditorId;

            await _repo.AddAuditActionAsync(new AuditActionItem
            {
                IdeaId = idea.Id,
                Decision = "route",
                DecisionText = decisionText,
                TargetDepartmentId = departmentId,
                AuditorId = auditorId ?? Guid.Empty
            }, ct);

            await _repo.SaveChangesAsync(ct);

            await _auditLog.WriteAsync("Audit.Route", "InnovationIdea", idea.Id.ToString(),
                $"AssignedDepartmentId={departmentId}", $"Status={idea.CurrentStatus?.Code}", ct);
            await SafeNotifyAsync("Audit.Route", idea.Id.ToString(), new Dictionary<string, string>
            {
                ["departmentId"] = departmentId.ToString(),
                ["departmentName"] = department.Name
            }, ct);

            return new(AuditActionOutcome.Success, $"تم تحويل الملف إلى إدارة: {department.Name}.");
        }

        public async Task<AuditActionResultDto> RejectAsync(Guid id, string? reason, Guid? auditorId, CancellationToken ct)
        {
            var idea = await _repo.GetForTransitionAsync(id, ct);
            if (idea is null) return new(AuditActionOutcome.NotFound, null);

            if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
                return new(AuditActionOutcome.InvalidInput, "يرجى إدخال سبب رفض لا يقل عن 10 أحرف.");

            if (idea.CurrentStatus?.Code == IdeaStatusCodes.Rejected || idea.CurrentStatus?.IsTerminal == true)
                return new(AuditActionOutcome.InvalidState, "الملف في حالة نهائية ولا يمكن رفضه مجدداً.");

            var rejectedId = await _repo.GetStatusIdByCodeAsync(IdeaStatusCodes.Rejected, ct);
            if (rejectedId is null)
                return new(AuditActionOutcome.InvalidState, "لم يتم إعداد حالة (مرفوض) بعد.");

            var fromId = idea.CurrentStatusId;
            var trimmedReason = reason.Trim();
            idea.CurrentStatusId = rejectedId.Value;

            await _repo.AddAuditActionAsync(new AuditActionItem
            {
                IdeaId = idea.Id,
                Decision = "reject",
                DecisionText = trimmedReason,
                AuditorId = auditorId ?? Guid.Empty
            }, ct);

            await _repo.AddStatusHistoryAsync(new IdeaStatusHistory
            {
                InnovationIdeaId = idea.Id,
                FromStatusId = fromId,
                ToStatusId = rejectedId.Value,
                ChangedByUserId = auditorId,
                Note = trimmedReason
            }, ct);

            await _repo.SaveChangesAsync(ct);
            await _auditLog.WriteAsync("Audit.Reject", "InnovationIdea", idea.Id.ToString(),
                $"Status={IdeaStatusCodes.Rejected}", $"Status={fromId}", ct);
            await SafeNotifyAsync("Audit.Reject", idea.Id.ToString(), new Dictionary<string, string>
            {
                ["reasonLength"] = trimmedReason.Length.ToString()
            }, ct);

            return new(AuditActionOutcome.Success, "تم رفض الملف.");
        }

        public async Task<AuditActionResultDto> RequestCompletionAsync(Guid id, string? instructions, Guid? auditorId, CancellationToken ct)
        {
            var idea = await _repo.GetForTransitionAsync(id, ct);
            if (idea is null) return new(AuditActionOutcome.NotFound, null);

            if (string.IsNullOrWhiteSpace(instructions) || instructions.Length < 10)
                return new(AuditActionOutcome.InvalidInput, "يرجى إدخال تعليمات الاستكمال التي لا تقل عن 10 أحرف.");

            if (idea.CurrentStatus?.IsTerminal == true)
                return new(AuditActionOutcome.InvalidState, "الملف في حالة نهائية ولا يمكن طلب استكماله.");

            var waitingId = await _repo.GetStatusIdByCodeAsync(IdeaStatusCodes.WaitingForCompletion, ct);
            if (waitingId is null)
                return new(AuditActionOutcome.InvalidState, "لم يتم إعداد حالة (بانتظار الاستكمال) بعد.");

            var fromId = idea.CurrentStatusId;
            var trimmed = instructions.Trim();
            idea.CurrentStatusId = waitingId.Value;

            await _repo.AddAuditActionAsync(new AuditActionItem
            {
                IdeaId = idea.Id,
                Decision = "request_completion",
                DecisionText = trimmed,
                AuditorId = auditorId ?? Guid.Empty
            }, ct);

            await _repo.AddStatusHistoryAsync(new IdeaStatusHistory
            {
                InnovationIdeaId = idea.Id,
                FromStatusId = fromId,
                ToStatusId = waitingId.Value,
                ChangedByUserId = auditorId,
                Note = trimmed
            }, ct);

            await _repo.SaveChangesAsync(ct);
            await _auditLog.WriteAsync("Audit.RequestCompletion", "InnovationIdea", idea.Id.ToString(),
                $"Status={IdeaStatusCodes.WaitingForCompletion}", $"Status={fromId}", ct);
            await SafeNotifyAsync("Audit.RequestCompletion", idea.Id.ToString(), null, ct);

            return new(AuditActionOutcome.Success, "تم طلب استكمال الملف من مقدمه.");
        }

        private async Task<IReadOnlyList<AuditInboxRowDto>> GetInboxRowsInternalAsync(string applicantTypeNorm, string statusNorm, CancellationToken ct)
        {
            var codes = statusNorm switch
            {
                "new" => new[] { IdeaStatusCodes.New },
                "resubmitted" => new[] { IdeaStatusCodes.Resubmitted },
                "rejected" => new[] { IdeaStatusCodes.Rejected },
                _ => new[] { IdeaStatusCodes.New, IdeaStatusCodes.Resubmitted }
            };
            return await _repo.GetInboxRowsAsync(applicantTypeNorm, codes, InboxTake, ct);
        }

        private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notify {Action} failed for {Entity}", action, entityId);
            }
        }
    }
}