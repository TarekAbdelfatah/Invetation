using Ibtikar.DTOs.Committee;
using Ibtikar.DTOs.MyRequests;
using Ibtikar.Models;
using Ibtikar.Repositories;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Interfaces;
using Ibtikar.Services.Notifications;

namespace Ibtikar.Services.Implementations
{
    public sealed class CommitteeDashboardService : ICommitteeDashboardService
    {
        private const int MinScore = 1;
        private const int MaxScore = 5;

        private readonly ICommitteeDashboardRepository _repo;
        private readonly ICommitteeRepository _committees;
        private readonly INotificationClient _notifier;
        private readonly ILogger<CommitteeDashboardService> _logger;

        public CommitteeDashboardService(
            ICommitteeDashboardRepository repo,
            ICommitteeRepository committees,
            INotificationClient notifier,
            ILogger<CommitteeDashboardService> logger)
        {
            _repo = repo;
            _committees = committees;
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

            return await _repo.GetSnapshotCountsAsync(ct);
        }

        public async Task<IReadOnlyList<CommitteeReferralRowDto>?> GetReferralsAsync(Guid userId, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null) return null;

            return await _repo.GetReferralsAsync(userId, ct);
        }

        public async Task<bool> IsActiveCommitteeMemberAsync(Guid userId, CancellationToken ct)
            => (await _committees.GetCommitteeIdForMemberAsync(userId, ct)).HasValue;

        public async Task<CommitteeAssessDto?> GetAssessAsync(Guid userId, Guid ideaId, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null) return null;

            var idea = await _repo.GetAssessIdeaAsync(ideaId, ct);
            if (idea is null) return null;

            var ideaReadOnly = await _repo.GetIdeaReadOnlyAsync(ideaId, ct);

            var criteria = await _repo.GetActiveCriteriaAsync(ct);

            var draft = await _repo.GetLatestCommitteeHeaderAsync(ideaId, userId, ct);
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
                idea.IdeaId, idea.Reference, idea.Title,
                idea.StatusName, idea.StatusColor,
                draftIsLatest, draft?.IsLocked ?? false,
                draft?.Id, draft?.CreatedAt,
                draft?.TotalScore, draft?.Comment,
                criteria, lines,
                departmentPercent, committeePercent, combined,
                ideaReadOnly ?? new CommitteeIdeaReadOnlyDto(
                    idea.Title, string.Empty, null, null, null, null,
                    null, null, null, null, null, false, null,
                    DateTime.UtcNow, null, new List<MyRequestAttachmentDto>()));
        }

        public async Task<CommitteeAssessOutcomeDto> SaveAssessmentAsync(
            Guid userId,
            CommitteeAssessmentSubmissionDto submission,
            CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null)
                return new(false, "لست عضواً نشطاً في أي لجنة.", false);

            if (!await _repo.IdeaExistsAsync(submission.IdeaId, ct))
                return new(false, "الفكرة غير موجودة.", false);

            if (submission.Scores.Count == 0)
                return new(false, "أدخل درجة واحدة على الأقل.", false);

            foreach (var s in submission.Scores)
            {
                if (s.Score < MinScore || s.Score > MaxScore)
                    return new(false, $"الدرجة يجب أن تكون بين {MinScore} و {MaxScore}.", false);
            }

            var criteriaCount = await _repo.CountActiveCriteriaAsync(ct);
            if (!submission.SaveDraft && submission.Scores.Count < criteriaCount)
                return new(false, "يجب إكمال تقييم جميع المعايير قبل الإرسال.", false);

            var header = await _repo.GetCommitteeHeaderForSaveAsync(submission.IdeaId, userId, submission.HeaderId, ct);

            if (header is null)
            {
                header = new AssessmentHeader
                {
                    Id = Guid.NewGuid(),
                    InnovationIdeaId = submission.IdeaId,
                    AssessorUserId = userId,
                    AssessorDepartmentId = null,
                    Source = AssessmentHeader.SourceCommittee,
                    CreatedAt = DateTime.UtcNow
                };
                _repo.AddAssessmentHeader(header);
            }
            else
            {
                _repo.RemoveAssessmentDetails(header.Details);
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
            await _repo.SaveChangesAsync(ct);

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

            var ideas = await _repo.GetVoteIdeasAsync(ct);
            var ideaIds = ideas.Select(i => i.IdeaId).ToList();
            var myVotes = await _repo.GetVotesByUserAsync(userId, ideaIds, ct);

            var items = ideas.Select(i => new CommitteeVoteRowDto(
                i.IdeaId, i.Reference, i.Title,
                i.StatusCode, i.StatusName, i.StatusColor,
                myVotes.ContainsKey(i.IdeaId),
                myVotes.TryGetValue(i.IdeaId, out var d) ? d : null,
                i.Description, i.ProblemStatement, i.ProposedSolution, i.ExpectedBenefits,
                i.Idea)).ToList();

            return new CommitteeVotesDto(items);
        }

        public async Task<CommitteeVoteRowDto?> GetSingleVoteAsync(Guid userId, Guid ideaId, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null) return null;

            var idea = await _repo.GetAssessIdeaAsync(ideaId, ct);
            if (idea is null) return null;

            var ideaReadOnly = await _repo.GetIdeaReadOnlyAsync(ideaId, ct);
            var myVote = await _repo.GetVotesByUserAsync(userId, new[] { ideaId }, ct);
            var hasVoted = myVote.ContainsKey(ideaId);

            return new CommitteeVoteRowDto(
                idea.IdeaId, idea.Reference, idea.Title,
                StatusCode: string.Empty, idea.StatusName, idea.StatusColor,
                hasVoted,
                hasVoted ? myVote[ideaId] : null,
                ideaReadOnly?.Description ?? string.Empty,
                ideaReadOnly?.ProblemStatement,
                ideaReadOnly?.ProposedSolution,
                ideaReadOnly?.ExpectedBenefits,
                ideaReadOnly ?? new CommitteeIdeaReadOnlyDto(
                    idea.Title, string.Empty, null, null, null, null,
                    null, null, null, null, null, false, null,
                    DateTime.UtcNow, null, new List<MyRequestAttachmentDto>()));
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

            if (!await _repo.IdeaExistsAsync(submission.IdeaId, ct))
                return new(false, "الفكرة غير موجودة.");

            var currentStatusId = await _repo.GetIdeaCurrentStatusIdAsync(submission.IdeaId, ct);
            if (currentStatusId.HasValue)
            {
                var code = await _repo.GetStatusCodeByIdAsync(currentStatusId.Value, ct);
                if (code is not null && code != IdeaStatusCodes.ReferredCommittee)
                    return new(false, "انتهى التصويت على هذه الفكرة أو أنها غير مفتوحة للتصويت.");
            }

            var alreadyVoted = await _repo.HasVotedAsync(submission.IdeaId, userId, ct);
            if (alreadyVoted)
                return new(false, "سبق لك التصويت على هذه الفكرة (تصويت واحد لكل عضو).");

            await _repo.AddVoteAsync(new CommitteeVote
            {
                Id = Guid.NewGuid(),
                InnovationIdeaId = submission.IdeaId,
                MemberUserId = userId,
                Decision = submission.Decision,
                VotedAt = DateTime.UtcNow
            }, ct);

            return new(true, "تم تسجيل تصويتك.");
        }

        public async Task<CommitteeDecisionDto?> GetDecisionAsync(Guid userId, Guid ideaId, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null) return null;

            var idea = await _repo.GetDecisionIdeaAsync(ideaId, ct);
            if (idea is null) return null;

            var combined = await GetCombinedPercentAsync(ideaId, ct);
            var canAccept = idea.StatusCode == IdeaStatusCodes.ReferredCommittee;
            var warning = combined < 40
                ? "النسبة المجمعة أقل من 40%. هل تريد الموافقة على الفكرة رغم ذلك؟"
                : null;

            return new CommitteeDecisionDto(idea.IdeaId, idea.Reference, idea.Title, combined, canAccept, warning);
        }

        public async Task<CommitteeVoteOutcomeDto> AcceptAsync(Guid userId, Guid ideaId, bool extraConfirmed, CancellationToken ct)
        {
            var committeeId = await GetCommitteeIdForMemberAsync(userId, ct);
            if (committeeId is null)
                return new(false, "لست عضواً نشطاً في أي لجنة.");

            var idea = await _repo.GetIdeaWithStatusAsync(ideaId, ct);
            if (idea is null)
                return new(false, "الفكرة غير موجودة.");

            if (idea.CurrentStatus?.Code != IdeaStatusCodes.ReferredCommittee)
                return new(false, "الفكرة ليست في حالة (محوّلة للجنة) للقبول.");

            var combined = await GetCombinedPercentAsync(ideaId, ct);
            if (combined < 40 && !extraConfirmed)
                return new(false, "النسبة المجمعة أقل من 40%. يلزم تأكيد إضافي لقبول الفكرة.");

            var approvedId = await _repo.GetStatusIdByCodeAsync(IdeaStatusCodes.Approved, ct);
            if (approvedId is null)
                return new(false, "لم يتم إعداد حالة (قبول الفكرة) بعد.");

            var fromId = idea.CurrentStatusId;
            idea.CurrentStatusId = approvedId.Value;

            await _repo.AddStatusHistoryAndSaveAsync(new IdeaStatusHistory
            {
                InnovationIdeaId = idea.Id,
                FromStatusId = fromId,
                ToStatusId = approvedId.Value,
                ChangedByUserId = userId,
                Note = combined < 40
                    ? $"قبول اللجنة مع تأكيد إضافي (نسبة مجمعة {combined}%)."
                    : $"قبول اللجنة (نسبة مجمعة {combined}%)."
            }, ct);

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

            var idea = await _repo.GetIdeaWithStatusAsync(ideaId, ct);
            if (idea is null)
                return new(false, "الفكرة غير موجودة.");

            if (idea.CurrentStatus?.Code != IdeaStatusCodes.ReferredCommittee)
                return new(false, "الفكرة ليست في حالة (محوّلة للجنة) للرفض.");

            var rejectedId = await _repo.GetStatusIdByCodeAsync(IdeaStatusCodes.Rejected, ct);
            if (rejectedId is null)
                return new(false, "لم يتم إعداد حالة (مرفوض) بعد.");

            var fromId = idea.CurrentStatusId;
            var trimmedReason = reason.Trim();
            idea.CurrentStatusId = rejectedId.Value;

            await _repo.AddStatusHistoryAndSaveAsync(new IdeaStatusHistory
            {
                InnovationIdeaId = idea.Id,
                FromStatusId = fromId,
                ToStatusId = rejectedId.Value,
                ChangedByUserId = userId,
                Note = trimmedReason
            }, ct);

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

            var idea = await _repo.GetIdeaWithStatusAsync(ideaId, ct);
            if (idea is null)
                return new(false, "الفكرة غير موجودة.");

            if (idea.CurrentStatus?.Code != IdeaStatusCodes.ReferredCommittee)
                return new(false, "الفكرة ليست في حالة (محوّلة للجنة) للإعادة للتطوير.");

            var combined = await GetCombinedPercentAsync(ideaId, ct);
            if (combined < 61 || combined > 79)
                return new(false, "الإعادة للتطوير مسموحة فقط عندما تكون النسبة المجمعة بين 61% و 79%.");

            var returnedId = await _repo.GetStatusIdByCodeAsync(IdeaStatusCodes.ReturnedForDevelopment, ct);
            if (returnedId is null)
                return new(false, "لم يتم إعداد حالة (معاد للتطوير) بعد.");

            var fromId = idea.CurrentStatusId;
            idea.CurrentStatusId = returnedId.Value;

            await _repo.AddStatusHistoryAndSaveAsync(new IdeaStatusHistory
            {
                InnovationIdeaId = idea.Id,
                FromStatusId = fromId,
                ToStatusId = returnedId.Value,
                ChangedByUserId = userId,
                Note = "إعادة للتطوير من اللجنة بناءً على تقييم الأعضاء."
            }, ct);

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
            var criteriaCount = await _repo.CountActiveCriteriaAsync(ct);
            if (criteriaCount == 0) return null;

            var committeeHeader = await _repo.GetLatestSubmittedHeaderAsync(ideaId, AssessmentHeader.SourceCommittee, ct);

            if (committeeHeader is null || committeeHeader.Details.Count == 0) return null;
            return CalculatePercent(committeeHeader.Details.Sum(d => d.Score), criteriaCount);
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
            var criteriaCount = await _repo.CountActiveCriteriaAsync(ct);
            if (criteriaCount == 0) return null;

            var specialized = await _repo.GetLatestSubmittedHeaderAsync(ideaId, AssessmentHeader.SourceSpecialized, ct);

            if (specialized is null || specialized.Details.Count == 0) return null;
            return CalculatePercent(specialized.Details.Sum(d => d.Score), criteriaCount);
        }

        private static int CalculatePercent(int scoreSum, int criteriaCount)
            => criteriaCount == 0
                ? 0
                : (int)Math.Round(scoreSum / (criteriaCount * (double)MaxScore) * 100, MidpointRounding.AwayFromZero);

        private static int CalculateCombined(int departmentPercent, int committeePercent)
            => (int)Math.Round((departmentPercent + committeePercent) / 2.0, MidpointRounding.AwayFromZero);

        private Task<Guid?> GetCommitteeIdForMemberAsync(Guid userId, CancellationToken ct)
            => _committees.GetCommitteeIdForMemberAsync(userId, ct);
    }
}