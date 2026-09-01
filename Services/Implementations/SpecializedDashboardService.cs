using Ibtikar.DTOs.SpecializedDashboard;
using Ibtikar.Models;
using Ibtikar.Repositories;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Interfaces;

namespace Ibtikar.Services.Implementations
{
    public sealed class SpecializedDashboardService : ISpecializedDashboardService
    {
        private const int MinScore = 1;
        private const int MaxScore = 5;

        private readonly ISpecializedDashboardRepository _repo;
        private readonly ILogger<SpecializedDashboardService> _logger;

        public SpecializedDashboardService(
            ISpecializedDashboardRepository repo,
            ILogger<SpecializedDashboardService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<SpecializedDashboardDto?> GetSnapshotAsync(Guid? departmentId, CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty) return null;
            return await _repo.GetSnapshotAsync(departmentId.Value, ct);
        }

        public async Task<SpecializedReferralsDto?> GetReferralsAsync(Guid? departmentId, string? status, int page, int pageSize, CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty) return null;
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            return await _repo.GetReferralsAsync(departmentId.Value, status ?? string.Empty, page, pageSize, ct);
        }

        public async Task<SpecializedDetailsDto?> GetDetailsAsync(Guid? departmentId, Guid ideaId, CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty) return null;
            return await _repo.GetDetailsAsync(ideaId, departmentId.Value, ct);
        }

        public async Task<SpecializedAssessVmDto?> GetAssessVmAsync(Guid? departmentId, Guid ideaId, CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty) return null;
            return await _repo.GetAssessVmAsync(ideaId, departmentId.Value, ct);
        }

        public async Task<SpecializedAssessmentOutcomeDto> SaveAssessmentAsync(
            Guid? departmentId,
            Guid actorUserId,
            SpecializedAssessmentSubmissionDto submission,
            CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty)
                return new(false, submission.IsDraft, "إدارة المستخدم غير معروفة.", null, null);

            var idea = await _repo.GetIdeaForDepartmentAsync(submission.IdeaId, departmentId.Value, ct);
            if (idea is null)
                return new(false, submission.IsDraft, "الفكرة غير موجودة أو غير محوّلة لإدارتك.", null, null);

            if (submission.Scores.Count == 0)
                return new(false, submission.IsDraft, "أدخل درجة واحدة على الأقل.", null, null);

            foreach (var s in submission.Scores)
            {
                if (s.Score < MinScore || s.Score > MaxScore)
                    return new(false, submission.IsDraft, $"الدرجة يجب أن تكون بين {MinScore} و {MaxScore}.", null, null);
            }

            var criteria = await _repo.GetAssessVmAsync(submission.IdeaId, departmentId.Value, ct);
            if (criteria is null)
                return new(false, submission.IsDraft, "لم يتم العثور على معايير التقييم.", null, null);

            var criterionIds = criteria.Criteria.Select(c => c.Id).ToHashSet();
            if (submission.Scores.Any(s => !criterionIds.Contains(s.CriterionId)))
                return new(false, submission.IsDraft, "أحد المعايير غير صالح.", null, null);

            if (!submission.IsDraft)
            {
                var allCovered = criteria.Criteria.All(c => submission.Scores.Any(s => s.CriterionId == c.Id));
                if (!allCovered)
                    return new(false, false, "يجب تقييم جميع المعايير قبل الإرسال.", null, null);
            }

            var header = submission.HeaderId.HasValue
                ? await _repo.GetDraftHeaderAsync(submission.IdeaId, departmentId.Value, ct)
                : null;

            var newHeader = header ?? new AssessmentHeader
            {
                Id = Guid.NewGuid(),
                InnovationIdeaId = submission.IdeaId,
                AssessorUserId = actorUserId,
                AssessorDepartmentId = departmentId.Value,
                Source = AssessmentHeader.SourceSpecialized,
                IsDraft = submission.IsDraft,
                CreatedAt = DateTime.UtcNow
            };

            newHeader.IsDraft = submission.IsDraft;
            newHeader.Comment = submission.Comment;
            newHeader.Details = submission.Scores.Select(s => new AssessmentDetail
            {
                Id = Guid.NewGuid(),
                CriterionId = s.CriterionId,
                Score = s.Score,
                Comment = s.Comment
            }).ToList();

            var activeCriteria = await GetActiveCriteriaPercentAsync(ct);
            decimal total = 0;
            foreach (var s in submission.Scores)
            {
                var pct = activeCriteria.TryGetValue(s.CriterionId, out var p) ? p : 0;
                total += s.Score * pct;
            }
            newHeader.TotalScore = total;

            if (!submission.IsDraft)
            {
                newHeader.IsLocked = true;
                newHeader.LockedAt = DateTime.UtcNow;
                newHeader.SubmittedAt = DateTime.UtcNow;
            }

            await _repo.AddOrUpdateAssessmentHeaderAsync(newHeader, ct);
            await _repo.SaveChangesAsync(ct);

            var message = submission.IsDraft
                ? "تم حفظ المسودة."
                : "تم إرسال التقييم.";
            return new(true, submission.IsDraft, message, newHeader.Id, total);
        }

        public async Task<SpecializedRequestDto?> GetRequestVmAsync(Guid? departmentId, Guid ideaId, CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty) return null;

            var details = await _repo.GetDetailsAsync(ideaId, departmentId.Value, ct);
            if (details is null) return null;

            var assigned = await _repo.GetAlreadyAssignedPartnersAsync(ideaId, ct);
            var assignedIds = assigned.Select(p => p.Id).ToList();
            var available = await _repo.GetAvailablePartnersAsync(departmentId.Value, assignedIds, ct);

            return new SpecializedRequestDto(
                details.Id, details.Reference, details.Title,
                available, assigned);
        }

        public async Task<SpecializedRequestOutcomeDto> RequestPartnerOpinionsAsync(
            Guid? departmentId,
            Guid actorUserId,
            SpecializedRequestSubmissionDto submission,
            CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty)
                return new(false, "إدارة المستخدم غير معروفة.", 0);

            var idea = await _repo.GetIdeaForDepartmentAsync(submission.IdeaId, departmentId.Value, ct);
            if (idea is null)
                return new(false, "الفكرة غير موجودة أو غير محوّلة لإدارتك.", 0);

            if (submission.PartnerDepartmentIds is null || submission.PartnerDepartmentIds.Count == 0)
                return new(false, "اختر إدارة شريكة واحدة على الأقل.", 0);

            if (submission.PartnerDepartmentIds.Count > 2)
                return new(false, "لا يمكن طلب رأي أكثر من إدارتين في المرة الواحدة.", 0);

            var assignedIds = (await _repo.GetAlreadyAssignedPartnersAsync(submission.IdeaId, ct))
                .Select(p => p.Id).ToHashSet();

            var newOnes = submission.PartnerDepartmentIds
                .Where(id => id != departmentId && !assignedIds.Contains(id))
                .Distinct()
                .ToList();

            if (newOnes.Count == 0)
                return new(false, "كل الإدارات المختارة تم إسنادها سابقاً.", 0);

            var notesByPartner = (submission.PartnerNotes ?? new List<SpecializedRequestPartnerNoteDto>())
                .Where(n => !string.IsNullOrWhiteSpace(n.Note))
                .GroupBy(n => n.PartnerDepartmentId)
                .ToDictionary(g => g.Key, g => g.First().Note);

            var rows = newOnes.Select(id => new PartnerAssignment
            {
                Id = Guid.NewGuid(),
                InnovationIdeaId = submission.IdeaId,
                PartnerDepartmentId = id,
                RequestedByUserId = actorUserId,
                SentAt = DateTime.UtcNow,
                Status = PartnerAssignment.StatusPending,
                Note = notesByPartner.TryGetValue(id, out var n) ? n : null
            }).ToList();

            await _repo.AddPartnerAssignmentsAsync(rows, ct);
            await _repo.SaveChangesAsync(ct);

            return new(true, $"تم إرسال الطلب إلى {rows.Count} إدارة شريكة.", rows.Count);
        }

        public async Task<SpecializedPartnerOpinionDto?> GetPartnerOpinionAsync(Guid? departmentId, Guid ideaId, CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty) return null;
            return await _repo.GetPartnerOpinionAsync(ideaId, departmentId.Value, ct);
        }

        public async Task<SpecializedSendToCommitteeDto?> GetSendToCommitteeSummaryAsync(Guid? departmentId, Guid ideaId, CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty) return null;
            return await _repo.GetSendToCommitteeSummaryAsync(ideaId, departmentId.Value, ct);
        }

        public async Task<SpecializedSendToCommitteeOutcomeDto> SendToCommitteeAsync(
            Guid? departmentId,
            Guid actorUserId,
            Guid ideaId,
            bool skipPartnerWarning,
            CancellationToken ct)
        {
            if (departmentId is null || departmentId == Guid.Empty)
                return new(false, "إدارة المستخدم غير معروفة.", false);

            var summary = await _repo.GetSendToCommitteeSummaryAsync(ideaId, departmentId.Value, ct);
            if (summary is null)
                return new(false, "الفكرة غير موجودة أو غير محوّلة لإدارتك.", false);

            if (summary.UnrepliedPartners > 0 && !skipPartnerWarning)
                return new(false, summary.WarningMessage, true);

            if (!summary.CanSend)
                return new(false, "يجب إكمال تقييم جميع المعايير الخمسة قبل الإرسال.", false);

            var idea = await _repo.GetIdeaForDepartmentAsync(ideaId, departmentId.Value, ct);
            if (idea is null)
                return new(false, "الفكرة غير موجودة.", false);

            var referredId = await GetStatusIdByCodeAsync(IdeaStatusCodes.ReferredCommittee, ct);
            if (referredId is null)
                return new(false, "لم يتم إعداد حالة (محال للجنة).", false);

            var fromId = idea.CurrentStatusId;
            idea.CurrentStatusId = referredId.Value;

            var note = $"إرسال إلى اللجنة بواسطة الإدارة. تم تقييم {summary.CompletedCriteria} من {summary.TotalCriteria} معيار.";
            _logger.LogInformation("Specialized sent {Idea} to committee (department={Dept}, user={User})",
                idea.ReferenceNumber, departmentId, actorUserId);

            await _repo.SaveChangesAsync(ct);
            return new(true, note, false);
        }

        private async Task<Dictionary<Guid, decimal>> GetActiveCriteriaPercentAsync(CancellationToken ct)
        {
            var scores = await Task.FromResult(new Dictionary<Guid, decimal>());
            return scores;
        }

        private async Task<Guid?> GetStatusIdByCodeAsync(string code, CancellationToken ct)
        {
            return await _repo.GetDraftHeaderAsync(Guid.Empty, Guid.Empty, ct) is null
                ? null
                : null;
        }
    }
}