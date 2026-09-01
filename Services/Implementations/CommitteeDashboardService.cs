using Ibtikar.Data;
using Ibtikar.DTOs.Committee;
using Ibtikar.Models;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Interfaces;
using Ibtikar.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Services.Implementations
{
    public sealed class CommitteeDashboardService : ICommitteeDashboardService
    {
        private const int MinScore = 1;
        private const int MaxScore = 5;

        private readonly IbtikarDbContext _db;
        private readonly INotificationClient _notifier;
        private readonly ILogger<CommitteeDashboardService> _logger;

        public CommitteeDashboardService(
            IbtikarDbContext db,
            INotificationClient notifier,
            ILogger<CommitteeDashboardService> logger)
        {
            _db = db;
            _notifier = notifier;
            _logger = logger;
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

        public async Task<CommitteeVotesDto?> GetVotesAsync(Guid userId, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null) return null;

            var ideas = await _db.InnovationIdeas.AsNoTracking()
                .Where(i => i.CurrentStatus != null
                    && (i.CurrentStatus.Code == IdeaStatusCodes.ReferredCommittee
                        || i.CurrentStatus.Code == IdeaStatusCodes.UnderAssessment))
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new
                {
                    i.Id,
                    i.ReferenceNumber,
                    i.Title,
                    StatusCode = i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty,
                    StatusName = i.CurrentStatus != null ? i.CurrentStatus.Name : "—",
                    StatusColor = i.CurrentStatus != null ? i.CurrentStatus.Color : "#6c757d"
                })
                .ToListAsync(ct);

            var ideaIds = ideas.Select(i => i.Id).ToList();
            var myVotes = await _db.CommitteeVotes.AsNoTracking()
                .Where(v => v.MemberUserId == userId && ideaIds.Contains(v.InnovationIdeaId))
                .ToDictionaryAsync(v => v.InnovationIdeaId, v => v.Decision, ct);

            var items = ideas.Select(i => new CommitteeVoteRowDto(
                i.Id, i.ReferenceNumber, i.Title,
                i.StatusCode, i.StatusName, i.StatusColor,
                myVotes.ContainsKey(i.Id),
                myVotes.TryGetValue(i.Id, out var d) ? d : null)).ToList();

            return new CommitteeVotesDto(items);
        }

        public async Task<CommitteeVoteOutcomeDto> SubmitVoteAsync(Guid userId, CommitteeVoteSubmitDto submission, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null)
                return new(false, "لست عضواً نشطاً في أي لجنة.");

            if (submission.Decision != CommitteeVote.DecisionAgree
                && submission.Decision != CommitteeVote.DecisionDisagree
                && submission.Decision != CommitteeVote.DecisionNeedsDevelopment)
            {
                return new(false, "قرار التصويت غير معروف.");
            }

            var idea = await _db.InnovationIdeas.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == submission.IdeaId, ct);
            if (idea is null)
                return new(false, "الفكرة غير موجودة.");

            var ideaStatus = await _db.IdeaStatuses.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == idea.CurrentStatusId, ct);
            if (ideaStatus is not null && ideaStatus.Code != IdeaStatusCodes.ReferredCommittee)
                return new(false, "انتهى التصويت على هذه الفكرة أو أنها غير مفتوحة للتصويت.");

            var alreadyVoted = await _db.CommitteeVotes.AsNoTracking()
                .AnyAsync(v => v.InnovationIdeaId == submission.IdeaId && v.MemberUserId == userId, ct);
            if (alreadyVoted)
                return new(false, "سبق لك التصويت على هذه الفكرة (تصويت واحد لكل عضو).");

            _db.CommitteeVotes.Add(new CommitteeVote
            {
                Id = Guid.NewGuid(),
                InnovationIdeaId = submission.IdeaId,
                MemberUserId = userId,
                Decision = submission.Decision,
                VotedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
            return new(true, "تم تسجيل تصويتك.");
        }

        public async Task<CommitteeDecisionDto?> GetDecisionAsync(Guid userId, Guid ideaId, CancellationToken ct)
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
                    StatusCode = i.CurrentStatus != null ? i.CurrentStatus.Code : string.Empty
                })
                .FirstOrDefaultAsync(ct);

            if (idea is null) return null;

            var combined = await GetCombinedPercentAsync(ideaId, ct);
            var canAccept = idea.StatusCode == IdeaStatusCodes.ReferredCommittee;
            var warning = combined < 40
                ? "النسبة المجمعة أقل من 40%. هل تريد الموافقة على الفكرة رغم ذلك؟"
                : null;

            return new CommitteeDecisionDto(idea.Id, idea.ReferenceNumber, idea.Title, combined, canAccept, warning);
        }

        public async Task<CommitteeVoteOutcomeDto> AcceptAsync(Guid userId, Guid ideaId, bool extraConfirmed, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null)
                return new(false, "لست عضواً نشطاً في أي لجنة.");

            var idea = await _db.InnovationIdeas
                .Include(i => i.CurrentStatus)
                .FirstOrDefaultAsync(i => i.Id == ideaId, ct);
            if (idea is null)
                return new(false, "الفكرة غير موجودة.");

            if (idea.CurrentStatus?.Code != IdeaStatusCodes.ReferredCommittee)
                return new(false, "الفكرة ليست في حالة (محوّلة للجنة) للقبول.");

            var combined = await GetCombinedPercentAsync(ideaId, ct);
            if (combined < 40 && !extraConfirmed)
                return new(false, "النسبة المجمعة أقل من 40%. يلزم تأكيد إضافي لقبول الفكرة.");

            var approvedId = await GetStatusIdByCodeAsync(IdeaStatusCodes.Approved, ct);
            if (approvedId is null)
                return new(false, "لم يتم إعداد حالة (قبول الفكرة) بعد.");

            var fromId = idea.CurrentStatusId;
            idea.CurrentStatusId = approvedId.Value;

            _db.IdeaStatusHistories.Add(new IdeaStatusHistory
            {
                InnovationIdeaId = idea.Id,
                FromStatusId = fromId,
                ToStatusId = approvedId.Value,
                ChangedByUserId = userId,
                Note = combined < 40
                    ? $"قبول اللجنة مع تأكيد إضافي (نسبة مجمعة {combined}%)."
                    : $"قبول اللجنة (نسبة مجمعة {combined}%)."
            });

            await _db.SaveChangesAsync(ct);

            await SafeNotifyAsync("Committee.Accept", idea.Id.ToString(), new Dictionary<string, string>
            {
                ["ideaId"] = idea.Id.ToString(),
                ["reference"] = idea.ReferenceNumber,
                ["title"] = idea.Title,
                ["combinedPercent"] = combined.ToString(),
                ["actorUserId"] = userId.ToString()
            }, ct);

            _logger.LogInformation("Committee accepted idea {Idea} (combined {Combined}%, extraConfirm={Extra})",
                idea.ReferenceNumber, combined, extraConfirmed);

            return new(true, $"تم قبول الفكرة. النسبة المجمعة: {combined}%.");
        }

        public async Task<CommitteeVoteOutcomeDto> RejectAsync(Guid userId, Guid ideaId, string reason, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null)
                return new(false, "لست عضواً نشطاً في أي لجنة.");

            if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
                return new(false, "يرجى إدخال سبب الرفض (10 أحرف على الأقل).");

            var idea = await _db.InnovationIdeas
                .Include(i => i.CurrentStatus)
                .FirstOrDefaultAsync(i => i.Id == ideaId, ct);
            if (idea is null)
                return new(false, "الفكرة غير موجودة.");

            if (idea.CurrentStatus?.Code != IdeaStatusCodes.ReferredCommittee)
                return new(false, "الفكرة ليست في حالة (محوّلة للجنة) للرفض.");

            var rejectedId = await GetStatusIdByCodeAsync(IdeaStatusCodes.Rejected, ct);
            if (rejectedId is null)
                return new(false, "لم يتم إعداد حالة (مرفوض) بعد.");

            var fromId = idea.CurrentStatusId;
            var trimmedReason = reason.Trim();
            idea.CurrentStatusId = rejectedId.Value;

            _db.IdeaStatusHistories.Add(new IdeaStatusHistory
            {
                InnovationIdeaId = idea.Id,
                FromStatusId = fromId,
                ToStatusId = rejectedId.Value,
                ChangedByUserId = userId,
                Note = trimmedReason
            });

            await _db.SaveChangesAsync(ct);

            await SafeNotifyAsync("Committee.Reject", idea.Id.ToString(), new Dictionary<string, string>
            {
                ["ideaId"] = idea.Id.ToString(),
                ["reference"] = idea.ReferenceNumber,
                ["title"] = idea.Title,
                ["reason"] = trimmedReason,
                ["actorUserId"] = userId.ToString()
            }, ct);

            _logger.LogInformation("Committee rejected idea {Idea} by user {User}", idea.ReferenceNumber, userId);

            return new(true, "تم رفض الفكرة.");
        }

        public async Task<CommitteeVoteOutcomeDto> ReturnForDevelopmentAsync(Guid userId, Guid ideaId, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null)
                return new(false, "لست عضواً نشطاً في أي لجنة.");

            var idea = await _db.InnovationIdeas
                .Include(i => i.CurrentStatus)
                .FirstOrDefaultAsync(i => i.Id == ideaId, ct);
            if (idea is null)
                return new(false, "الفكرة غير موجودة.");

            if (idea.CurrentStatus?.Code != IdeaStatusCodes.ReferredCommittee)
                return new(false, "الفكرة ليست في حالة (محوّلة للجنة) للإعادة للتطوير.");

            var combined = await GetCombinedPercentAsync(ideaId, ct);
            if (combined < 61 || combined > 79)
                return new(false, "الإعادة للتطوير مسموحة فقط عندما تكون النسبة المجمعة بين 61% و 79%.");

            var returnedId = await GetStatusIdByCodeAsync(IdeaStatusCodes.ReturnedForDevelopment, ct);
            if (returnedId is null)
                return new(false, "لم يتم إعداد حالة (معاد للتطوير) بعد.");

            var fromId = idea.CurrentStatusId;
            idea.CurrentStatusId = returnedId.Value;

            _db.IdeaStatusHistories.Add(new IdeaStatusHistory
            {
                InnovationIdeaId = idea.Id,
                FromStatusId = fromId,
                ToStatusId = returnedId.Value,
                ChangedByUserId = userId,
                Note = "إعادة للتطوير من اللجنة بناءً على تقييم الأعضاء."
            });

            await _db.SaveChangesAsync(ct);

            await SafeNotifyAsync("Committee.ReturnForDevelopment", idea.Id.ToString(), new Dictionary<string, string>
            {
                ["ideaId"] = idea.Id.ToString(),
                ["reference"] = idea.ReferenceNumber,
                ["title"] = idea.Title,
                ["combinedPercent"] = combined.ToString(),
                ["actorUserId"] = userId.ToString()
            }, ct);

            _logger.LogInformation("Committee returned idea {Idea} for development (combined {Combined}%)",
                idea.ReferenceNumber, combined);

            return new(true, $"تمت إعادة الفكرة للتطوير (النسبة المجمعة {combined}%).");
        }

        private async Task<int> GetCombinedPercentAsync(Guid ideaId, CancellationToken ct)
        {
            var departmentPercent = await GetSpecializedPercentAsync(ideaId, ct);
            var committeePercent = await GetCommitteePercentAsync(ideaId, ct);

            if (departmentPercent.HasValue && committeePercent.HasValue)
                return CalculateCombined(departmentPercent.Value, committeePercent.Value);
            if (departmentPercent.HasValue)
                return departmentPercent.Value;
            return committeePercent ?? 0;
        }

        private async Task<int?> GetCommitteePercentAsync(Guid ideaId, CancellationToken ct)
        {
            var criteriaCount = await _db.AssessmentCriteria.CountAsync(c => c.IsActive, ct);
            if (criteriaCount == 0) return null;

            var committeeHeader = await _db.AssessmentHeaders.AsNoTracking()
                .Include(h => h.Details)
                .Where(h => h.InnovationIdeaId == ideaId
                    && h.Source == AssessmentHeader.SourceCommittee
                    && !h.IsDraft)
                .OrderByDescending(h => h.SubmittedAt ?? h.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (committeeHeader is null || committeeHeader.Details.Count == 0) return null;
            return CalculatePercent(committeeHeader.Details.Sum(d => d.Score), criteriaCount);
        }

        private async Task<Guid?> GetStatusIdByCodeAsync(string code, CancellationToken ct)
        {
            return await _db.IdeaStatuses.AsNoTracking()
                .Where(s => s.Code == code)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(ct);
        }

        private async Task SafeNotifyAsync(string action, string entityId, IDictionary<string, string>? payload, CancellationToken ct)
        {
            try
            {
                await _notifier.SendAsync(action, entityId, payload, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // request cancelled; accept already committed and must not roll back.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notify {Action} failed for {Entity}", action, entityId);
            }
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
