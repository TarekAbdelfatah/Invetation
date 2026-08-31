using Ibtikar.DTOs.PartnerDashboard;
using Ibtikar.Models;
using Ibtikar.Repositories;

namespace Ibtikar.Services.PartnerDashboard
{
    public sealed class PartnerDashboardService : IPartnerDashboardService
    {
        private const int MinScore = 1;
        private const int MaxScore = 5;

        private readonly IPartnerDashboardRepository _repo;
        private readonly ILogger<PartnerDashboardService> _logger;

        public PartnerDashboardService(
            IPartnerDashboardRepository repo,
            ILogger<PartnerDashboardService> logger)
        {
            _repo = repo;
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
            return await _repo.GetDetailsAsync(assignmentId, departmentId.Value, ct);
        }

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

            assignment.Status = submission.ReturnOnly
                ? PartnerAssignment.StatusReturned
                : PartnerAssignment.StatusSubmitted;
            assignment.RespondedAt = DateTime.UtcNow;

            await _repo.SaveChangesAsync(ct);

            _logger.LogInformation("Partner {Dept} {Action} assessment for idea {Idea}",
                departmentId, submission.ReturnOnly ? "returned" : "submitted", assignment.InnovationIdeaId);

            var message = submission.ReturnOnly
                ? "تم إرجاع الطلب للإدارة المختصة."
                : "تم إرسال التقييم الاستشاري.";
            return new(true, message, total);
        }
    }
}