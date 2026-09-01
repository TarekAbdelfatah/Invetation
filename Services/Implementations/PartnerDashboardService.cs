using Ibtikar.DTOs.PartnerDashboard;
using Ibtikar.Models;
using Ibtikar.Repositories;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Notifications;

namespace Ibtikar.Services.Implementations
{
    public sealed class PartnerDashboardService : IPartnerDashboardService
    {
        private const int MinScore = 1;
        private const int MaxScore = 5;
        private const int NotCompetentWindowDays = 3;

        private readonly IPartnerDashboardRepository _repo;
        private readonly INotificationClient _notifier;
        private readonly ILogger<PartnerDashboardService> _logger;

        public PartnerDashboardService(
            IPartnerDashboardRepository repo,
            INotificationClient notifier,
            ILogger<PartnerDashboardService> logger)
        {
            _repo = repo;
            _notifier = notifier;
            _logger = logger;
        }

        public async Task<PartnerDashboardDto?> GetSnapshotAsync(Guid? departmentId, CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty) return null;
            return await _repo.GetSnapshotAsync(departmentId.Value, ct);
        }

        public async Task<PartnerInboxDto?> GetInboxAsync(Guid? departmentId, CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty) return null;
            return await _repo.GetInboxAsync(departmentId.Value, ct);
        }

        public async Task<PartnerDetailsDto?> GetDetailsAsync(Guid? departmentId, Guid assignmentId, CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty) return null;
            var dto = await _repo.GetDetailsAsync(assignmentId, departmentId.Value, ct);
            if (dto is null) return null;

            if (!dto.IsNotCompetentReturn && dto.Status == PartnerAssignment.StatusPending)
            {
                var now = DateTime.UtcNow;
                return dto with { CanReturnNotCompetent = IsReturnNotCompetentAllowed(dto.SentAt, now) };
            }

            return dto;
        }

        public bool IsReturnNotCompetentAllowed(DateTime sentAtUtc, DateTime nowUtc)
            => WorkingDays.IsWithinWindow(sentAtUtc, NotCompetentWindowDays, nowUtc);

        public async Task<PartnerSubmitOutcomeDto> SubmitAsync(
            Guid? departmentId,
            Guid actorUserId,
            PartnerSubmitDto submission,
            CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty)
                return new(false, "إدارة المستخدم غير معروفة.", null);

            var assignment = await _repo.GetAssignmentForPartnerAsync(submission.AssignmentId, departmentId.Value, ct);
            if (assignment is null)
                return new(false, "الإسناد غير موجود أو ليس لإدارتك.", null);

            if (assignment.Status == PartnerAssignment.StatusReturned)
                return new(false, "تم إرجاع هذا الطلب ولا يمكن تعديل التقييم.", null);

            if (assignment.Status == PartnerAssignment.StatusSubmitted)
                return new(false, "تم إرسال التقييم مسبقاً ولا يمكن تعديله.", null);

            if (submission.ReturnOnly && string.IsNullOrWhiteSpace(submission.Comment))
                return new(false, "يرجى كتابة مرئيات وملاحظات إدارتك قبل إعادته للإدارة صاحبة الفكرة.", null);

            if (!submission.ReturnOnly && submission.Scores.Count == 0)
                return new(false, "أدخل درجة واحدة على الأقل.", null);

            if (!submission.ReturnOnly)
            {
                foreach (var s in submission.Scores)
                {
                    if (s.Score < MinScore || s.Score > MaxScore)
                        return new(false, $"الدرجة يجب أن تكون بين {MinScore} و {MaxScore}.", null);
                }
            }

            var header = await _repo.GetExistingPartnerHeaderAsync(assignment.InnovationIdeaId, departmentId.Value, ct);
            header ??= new AssessmentHeader
            {
                Id = Guid.NewGuid(),
                InnovationIdeaId = assignment.InnovationIdeaId,
                AssessorUserId = actorUserId,
                AssessorDepartmentId = departmentId.Value,
                Source = AssessmentHeader.SourcePartner,
                CreatedAt = DateTime.UtcNow
            };

            header.IsDraft = false;
            header.IsLocked = !submission.ReturnOnly;
            header.Comment = submission.Comment;
            header.SubmittedAt = DateTime.UtcNow;
            header.LockedAt = submission.ReturnOnly ? null : DateTime.UtcNow;

            decimal total = 0;
            if (!submission.ReturnOnly)
            {
                header.Details = submission.Scores.Select(s => new AssessmentDetail
                {
                    Id = Guid.NewGuid(),
                    CriterionId = s.CriterionId,
                    Score = s.Score,
                    Comment = s.Comment
                }).ToList();

                total = header.Details.Sum(d => d.Score);
            }

            header.TotalScore = total;

            await _repo.AddOrUpdatePartnerHeaderAsync(header, ct);

            var previousStatus = assignment.Status;
            assignment.Status = submission.ReturnOnly
                ? PartnerAssignment.StatusReturned
                : PartnerAssignment.StatusSubmitted;
            assignment.RespondedAt = DateTime.UtcNow;

            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation("Partner {Dept} {Action} assessment for idea {Idea} (assignment {Assignment})",
                departmentId, submission.ReturnOnly ? "returned" : "submitted",
                assignment.InnovationIdeaId, assignment.Id);

            var action = submission.ReturnOnly ? "Partner.Return" : "Partner.Submit";
            await SafeNotifyAsync(action, assignment.Id.ToString(), new Dictionary<string, string>
            {
                ["ideaId"] = assignment.InnovationIdeaId.ToString(),
                ["departmentId"] = departmentId.Value.ToString(),
                ["actorUserId"] = actorUserId.ToString(),
                ["previousStatus"] = previousStatus,
                ["newStatus"] = assignment.Status,
                ["returnOnly"] = submission.ReturnOnly ? "true" : "false",
                ["totalScore"] = total.ToString("0.##")
            }, ct);

            var message = submission.ReturnOnly
                ? "تم إرجاع الطلب للإدارة صاحبة الفكرة."
                : "تم إرسال التقييم الاستشاري.";
            return new(true, message, total);
        }

        public async Task<PartnerSubmitOutcomeDto> ReturnNotCompetentAsync(
            Guid? departmentId,
            Guid actorUserId,
            Guid assignmentId,
            string reason,
            CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty)
                return new(false, "إدارة المستخدم غير معروفة.", null);

            if (string.IsNullOrWhiteSpace(reason))
                return new(false, "يرجى كتابة سبب الإعادة (خطأ في التوجيه).", null);

            var assignment = await _repo.GetAssignmentForPartnerAsync(assignmentId, departmentId.Value, ct);
            if (assignment is null)
                return new(false, "الإسناد غير موجود أو ليس لإدارتك.", null);

            if (assignment.Status == PartnerAssignment.StatusReturned)
                return new(false, "تم إرجاع هذا الطلب من قبل.", null);

            var now = DateTime.UtcNow;
            if (!IsReturnNotCompetentAllowed(assignment.SentAt, now))
            {
                var elapsed = WorkingDays.CountFrom(assignment.SentAt, now);
                return new(false,
                    $"انتهت مهلة الإعادة لعدم الاختصاص ({elapsed} أيام عمل من {NotCompetentWindowDays}).",
                    null);
            }

            assignment.Status = PartnerAssignment.StatusReturned;
            assignment.RespondedAt = now;
            assignment.Note = $"NotCompetent: {reason.Trim()}";

            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Partner {Dept} marked assignment {Assignment} as not-competent within {Window}-day window",
                departmentId, assignment.Id, NotCompetentWindowDays);

            return new(true, "تم إرجاع الطلب للإدارة صاحبة الفكرة بسبب خطأ في التوجيه.", null);
        }

        private async Task SafeNotifyAsync(string action, string entityId, IDictionary<string, string>? payload, CancellationToken ct)
        {
            try
            {
                await _notifier.SendAsync(action, entityId, payload, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // request cancelled; submit has already been committed and must not roll back.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notify {Action} failed for {Entity}", action, entityId);
            }
        }
    }
}