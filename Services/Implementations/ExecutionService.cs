using Ibtikar.Data;
using Ibtikar.DTOs.Execution;
using Ibtikar.Models;
using Ibtikar.Repositories;
using Ibtikar.Services.Interfaces;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Services.Implementations
{
    public sealed class ExecutionService : IExecutionService
    {
        private readonly IExecutionRepository _repo;
        private readonly IbtikarDbContext _db;
        private readonly AuditLogService _auditLog;
        private readonly INotificationClient _notifier;
        private readonly ILogger<ExecutionService> _logger;

        public ExecutionService(
            IExecutionRepository repo,
            IbtikarDbContext db,
            AuditLogService auditLog,
            INotificationClient notifier,
            ILogger<ExecutionService> logger)
        {
            _repo = repo;
            _db = db;
            _auditLog = auditLog;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<ExecutionListDto> GetListAsync(Guid? departmentId, CancellationToken ct)
            => await _repo.GetListAsync(departmentId, ct);

        public async Task<ExecutionHeaderDto?> GetHeaderAsync(Guid ideaId, Guid? departmentId, CancellationToken ct)
            => await _repo.GetHeaderAsync(ideaId, departmentId, ct);

        public async Task<ExecutionTimelineDto?> GetTimelineAsync(Guid ideaId, Guid? departmentId, CancellationToken ct)
            => await _repo.GetTimelineAsync(ideaId, departmentId, ct);

        public async Task<ExecutionActionOutcomeDto> UpdateStageAsync(
            ExecutionUpdateDto dto,
            Guid? userId,
            Guid? departmentId,
            CancellationToken ct)
        {
            if (dto is null)
                return new(false, "بيانات التحديث غير صحيحة.");

            if (string.IsNullOrWhiteSpace(dto.Note) || dto.Note.Trim().Length < 5)
                return new(false, "يرجى إدخال عبارة موجزة تصف الإنجاز الحالي قبل الحفظ.");

            if (!await _repo.IsAssigneeAsync(dto.IdeaId, departmentId, ct))
                return new(false, "لا تملك صلاحية تعديل هذه الفكرة.");

            var idea = await _db.InnovationIdeas
                .Include(i => i.CurrentStatus)
                .FirstOrDefaultAsync(i => i.Id == dto.IdeaId, ct);
            if (idea is null) return new(false, "لم يتم العثور على الفكرة.");

            if (idea.IsDraft)
                return new(false, "لا يمكن تحديث فكرة مسودة.");

            if (idea.CurrentStatus?.Code != IdeaStatusCodes.InExecution)
                return new(false, "الفكرة ليست في حالة تنفيذ.");

            var stage = await _db.ExecutionStages
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == dto.ExecutionStageId, ct);
            if (stage is null || !stage.IsActive)
                return new(false, "المرحلة المختارة غير صالحة.");

            _db.ExecutionProgresses.Add(new ExecutionProgress
            {
                InnovationIdeaId = idea.Id,
                ExecutionStageId = stage.Id,
                Note = dto.Note.Trim(),
                ChangedByUserId = userId,
                ChangedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            await _auditLog.WriteAsync(
                "Execution.StageUpdate",
                "InnovationIdea",
                idea.Id.ToString(),
                $"Stage={stage.Code}",
                string.Empty,
                ct);

            await SafeNotifyAsync("Execution.StageUpdate", idea.Id.ToString(), new Dictionary<string, string>
            {
                ["stage"] = stage.Code,
                ["stageName"] = stage.Name
            }, ct);

            return new(true, $"تم تسجيل المرحلة: {stage.Name}.");
        }

        public async Task<ExecutionActionOutcomeDto> CompleteAsync(
            ExecutionCompleteDto dto,
            Guid? userId,
            Guid? departmentId,
            CancellationToken ct)
        {
            if (dto is null) return new(false, "بيانات التحديث غير صحيحة.");
            if (string.IsNullOrWhiteSpace(dto.Note) || dto.Note.Trim().Length < 5)
                return new(false, "يرجى إدخال ملخص ما تم تنفيذه قبل الحفظ.");

            if (dto.AttachmentIds is null || dto.AttachmentIds.Count != 2)
                return new(false, "تتطلب مرحلة (تم التنفيذ) رفع ملفين PDF اثنين.");

            if (!await _repo.IsAssigneeAsync(dto.IdeaId, departmentId, ct))
                return new(false, "لا تملك صلاحية إكمال هذه الفكرة.");

            var idea = await _db.InnovationIdeas
                .Include(i => i.CurrentStatus)
                .FirstOrDefaultAsync(i => i.Id == dto.IdeaId, ct);
            if (idea is null) return new(false, "لم يتم العثور على الفكرة.");

            if (idea.IsDraft)
                return new(false, "لا يمكن إكمال فكرة مسودة.");

            if (idea.CurrentStatus?.Code != IdeaStatusCodes.InExecution)
                return new(false, "الفكرة ليست في حالة تنفيذ.");

            var stage = await _db.ExecutionStages
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == dto.CompletionStageId, ct);
            if (stage is null || !stage.IsActive)
                return new(false, "مرحلة الإكمال غير صالحة.");

            var attachments = await _db.IdeaAttachments
                .Where(a => dto.AttachmentIds.Contains(a.Id) && a.InnovationIdeaId == idea.Id)
                .ToListAsync(ct);
            if (attachments.Count != 2)
                return new(false, "يجب أن يكون الملفان مرفقان على نفس الفكرة.");
            if (attachments.Any(a => !a.ContentType.Contains("pdf")))
                return new(false, "يجب أن يكون الملفان بصيغة PDF.");

            _db.ExecutionProgresses.Add(new ExecutionProgress
            {
                InnovationIdeaId = idea.Id,
                ExecutionStageId = stage.Id,
                Note = dto.Note.Trim(),
                ChangedByUserId = userId,
                ChangedAt = DateTime.UtcNow
            });

            var completedId = await _repo.GetCompletedStatusIdAsync(ct);
            if (completedId is null)
                return new(false, "لم يتم إعداد حالة (مكتملة) بعد.");

            var fromId = idea.CurrentStatusId;
            idea.CurrentStatusId = completedId.Value;

            await _db.IdeaStatusHistories.AddAsync(new IdeaStatusHistory
            {
                InnovationIdeaId = idea.Id,
                FromStatusId = fromId,
                ToStatusId = completedId.Value,
                ChangedByUserId = userId,
                Note = "تم تنفيذ الفكرة وإرفاق ملفَي الإغلاق."
            }, ct);

            await _db.SaveChangesAsync(ct);
            await _auditLog.WriteAsync(
                "Execution.Complete",
                "InnovationIdea",
                idea.Id.ToString(),
                $"Status={IdeaStatusCodes.Completed}",
                $"Status={IdeaStatusCodes.InExecution}",
                ct);

            await SafeNotifyAsync("Execution.Complete", idea.Id.ToString(), null, ct);

            return new(true, "تم تنفيذ الفكرة وتسجيلها ضمن المكتملة.");
        }

        private async Task SafeNotifyAsync(string action, string entityId, IDictionary<string, string>? payload, CancellationToken ct)
        {
            try
            {
                await _notifier.SendAsync(action, entityId, payload, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notify {Action} failed for {Entity}", action, entityId);
            }
        }
    }
}
