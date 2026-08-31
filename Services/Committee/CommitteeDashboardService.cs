using Ibtikar.Data;
using Ibtikar.DTOs.Committee;
using Ibtikar.Models;
using Ibtikar.Services.Ideas;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Services.Committee
{
    public sealed class CommitteeDashboardService : ICommitteeDashboardService
    {
        private const int MinScore = 1;
        private const int MaxScore = 5;

        private readonly IbtikarDbContext _db;

        public CommitteeDashboardService(IbtikarDbContext db)
        {
            _db = db;
        }

        public async Task<CommitteeDashboardDto> GetSnapshotAsync(Guid userId, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null)
            {
                return new CommitteeDashboardDto(0, 0, 0, 0);
            }

            var underStudy = await _db.InnovationIdeas.AsNoTracking()
                .CountAsync(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.ReferredCommittee, ct);

            var underVoting = await _db.InnovationIdeas.AsNoTracking()
                .CountAsync(i => i.CurrentStatus != null
                    && (i.CurrentStatus.Code == IdeaStatusCodes.ReferredCommittee
                        || i.CurrentStatus.Code == IdeaStatusCodes.UnderAssessment), ct);

            var accepted = await _db.InnovationIdeas.AsNoTracking()
                .CountAsync(i => i.CurrentStatus != null
                    && (i.CurrentStatus.Code == IdeaStatusCodes.Approved
                        || i.CurrentStatus.Code == IdeaStatusCodes.InExecution), ct);

            var rejected = await _db.InnovationIdeas.AsNoTracking()
                .CountAsync(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.Rejected, ct);

            return new CommitteeDashboardDto(underStudy, underVoting, accepted, rejected);
        }

        public async Task<CommitteeReferralsDto?> GetReferralsAsync(Guid userId, string statusFilter, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null) return null;

            var query = _db.InnovationIdeas.AsNoTracking()
                .Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.ReferredCommittee);

            query = statusFilter switch
            {
                "accepted" => query.Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.Approved),
                "rejected" => query.Where(i => i.CurrentStatus != null && i.CurrentStatus.Code == IdeaStatusCodes.Rejected),
                _ => query
            };

            var now = DateTime.UtcNow;
            var rows = await query
                .OrderByDescending(i => i.CreatedAt)
                .Take(100)
                .Select(i => new CommitteeReferralRowDto(
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty,
                    i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d",
                    i.ApplicantUser != null ? i.ApplicantUser.FullName : null,
                    i.ApplicantDepartment != null ? i.ApplicantDepartment.Name : null,
                    i.AuditAssignedAt,
                    i.AuditAssignedAt.HasValue ? (now - i.AuditAssignedAt.Value).TotalDays : 0.0,
                    i.AuditAssignedAt.HasValue && (now - i.AuditAssignedAt.Value) > TimeSpan.FromDays(4)))
                .ToListAsync(ct);

            return new CommitteeReferralsDto(rows, statusFilter);
        }

        public Task<bool> IsActiveCommitteeMemberAsync(Guid userId, CancellationToken ct)
        {
            return _db.CommitteeMembers.AsNoTracking()
                .AnyAsync(m => m.UserId == userId && m.InnovationCommittee != null && m.InnovationCommittee.IsActive, ct);
        }

        public async Task<CommitteeAssessDto?> GetAssessAsync(Guid userId, Guid ideaId, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null) return null;

            var idea = await _db.InnovationIdeas.AsNoTracking()
                .Where(i => i.Id == ideaId)
                .Select(i => new
                {
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    StatusName = i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    StatusColor = i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d"
                })
                .FirstOrDefaultAsync(ct);

            if (idea is null) return null;

            var criteria = await _db.AssessmentCriteria.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CommitteeCriterionDto(c.Id, c.Code, c.Name, c.Description, c.DisplayOrder))
                .ToListAsync(ct);

            var draft = await _db.AssessmentHeaders.AsNoTracking()
                .Include(h => h.Details)
                .Where(h => h.InnovationIdeaId == ideaId
                    && h.AssessorUserId == userId
                    && h.Source == AssessmentHeader.SourceCommittee)
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var draftIsLatest = draft is { IsDraft: true, IsLocked: false };
            var lineMap = draftIsLatest && draft is not null
                ? draft.Details.ToDictionary(d => d.CriterionId, d => (d.Score, d.Comment))
                : new Dictionary<Guid, (int, string?)>();

            var lines = criteria.Select(c => lineMap.TryGetValue(c.Id, out var v)
                ? new CommitteeAssessLineDto(c.Id, c.Code, c.Name, v.Item1, v.Item2)
                : new CommitteeAssessLineDto(c.Id, c.Code, c.Name, null, null))
                .ToList();

            var departmentPercent = await GetSpecializedPercentAsync(ideaId, ct);
            var committeePercent = draftIsLatest && draft is { Details.Count: > 0 }
                ? CalculatePercent(draft.Details.Sum(d => d.Score), criteria.Count)
                : (int?)null;
            var combined = departmentPercent.HasValue && committeePercent.HasValue
                ? CalculateCombined(departmentPercent.Value, committeePercent.Value)
                : (int?)null;

            return new CommitteeAssessDto(
                idea.Id, idea.ReferenceNumber, idea.Title,
                idea.StatusName, idea.StatusColor,
                draftIsLatest, draft?.IsLocked ?? false,
                draft?.Id, draft?.CreatedAt,
                draft?.TotalScore, draft?.Comment,
                criteria, lines,
                departmentPercent, committeePercent, combined);
        }

        public async Task<CommitteeAssessOutcomeDto> SaveAssessmentAsync(
            Guid userId,
            CommitteeAssessmentSubmissionDto submission,
            CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null)
                return new(false, "لست عضواً نشطاً في أي لجنة.", false);

            var idea = await _db.InnovationIdeas.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == submission.IdeaId, ct);
            if (idea is null)
                return new(false, "الفكرة غير موجودة.", false);

            if (submission.Scores.Count == 0)
                return new(false, "أدخل درجة واحدة على الأقل.", false);

            foreach (var s in submission.Scores)
            {
                if (s.Score < MinScore || s.Score > MaxScore)
                    return new(false, $"الدرجة يجب أن تكون بين {MinScore} و {MaxScore}.", false);
            }

            var criteriaCount = await _db.AssessmentCriteria.CountAsync(c => c.IsActive, ct);
            if (!submission.SaveDraft && submission.Scores.Count < criteriaCount)
                return new(false, "يجب إكمال تقييم جميع المعايير قبل الإرسال.", false);

            var header = await _db.AssessmentHeaders
                .Include(h => h.Details)
                .FirstOrDefaultAsync(h => h.Id == submission.HeaderId, ct)
                ?? await _db.AssessmentHeaders
                    .Include(h => h.Details)
                    .Where(h => h.InnovationIdeaId == submission.IdeaId
                        && h.AssessorUserId == userId
                        && h.Source == AssessmentHeader.SourceCommittee)
                    .OrderByDescending(h => h.CreatedAt)
                    .FirstOrDefaultAsync(ct);

            if (header is null)
            {
                header = new AssessmentHeader
                {
                    Id = Guid.NewGuid(),
                    InnovationIdeaId = submission.IdeaId,
                    AssessorUserId = userId,
                    AssessorDepartmentId = Guid.Empty,
                    Source = AssessmentHeader.SourceCommittee,
                    CreatedAt = DateTime.UtcNow
                };
                _db.AssessmentHeaders.Add(header);
            }
            else
            {
                _db.AssessmentDetails.RemoveRange(header.Details);
            }

            header.IsDraft = submission.SaveDraft;
            header.IsLocked = !submission.SaveDraft;
            header.Comment = submission.Comment;
            header.SubmittedAt = submission.SaveDraft ? null : DateTime.UtcNow;
            header.LockedAt = submission.SaveDraft ? null : DateTime.UtcNow;

            header.Details = submission.Scores.Select(s => new AssessmentDetail
            {
                Id = Guid.NewGuid(),
                CriterionId = s.CriterionId,
                Score = s.Score,
                Comment = s.Comment
            }).ToList();

            header.TotalScore = submission.Scores.Sum(s => s.Score);
            await _db.SaveChangesAsync(ct);

            var departmentPercent = await GetSpecializedPercentAsync(submission.IdeaId, ct);
            var committeePercent = CalculatePercent((int)header.TotalScore.Value, criteriaCount);
            var combined = departmentPercent.HasValue
                ? CalculateCombined(departmentPercent.Value, committeePercent)
                : committeePercent;

            var message = submission.SaveDraft
                ? "تم حفظ مسودة التقييم."
                : $"تم إرسال تقييم اللجنة. النسبة المجمعة: {combined}%.";

            return new(true, message, combined < 40);
        }

        private async Task<int?> GetSpecializedPercentAsync(Guid ideaId, CancellationToken ct)
        {
            var criteriaCount = await _db.AssessmentCriteria.CountAsync(c => c.IsActive, ct);
            if (criteriaCount == 0) return null;

            var specialized = await _db.AssessmentHeaders.AsNoTracking()
                .Include(h => h.Details)
                .Where(h => h.InnovationIdeaId == ideaId
                    && h.Source == AssessmentHeader.SourceSpecialized
                    && !h.IsDraft)
                .OrderByDescending(h => h.SubmittedAt ?? h.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (specialized is null || specialized.Details.Count == 0) return null;
            return CalculatePercent(specialized.Details.Sum(d => d.Score), criteriaCount);
        }

        private static int CalculatePercent(int scoreSum, int criteriaCount)
            => criteriaCount == 0
                ? 0
                : (int)Math.Round(scoreSum / (criteriaCount * (double)MaxScore) * 100, MidpointRounding.AwayFromZero);

        private static int CalculateCombined(int departmentPercent, int committeePercent)
            => (int)Math.Round((departmentPercent + committeePercent) / 2.0, MidpointRounding.AwayFromZero);

        private async Task<Guid?> GetCommitteeIdForMemberAsync(Guid userId, CancellationToken ct)
        {
            return await _db.CommitteeMembers.AsNoTracking()
                .Where(m => m.UserId == userId && m.InnovationCommittee != null && m.InnovationCommittee.IsActive)
                .Select(m => (Guid?)m.InnovationCommitteeId)
                .FirstOrDefaultAsync(ct);
        }
    }
}
