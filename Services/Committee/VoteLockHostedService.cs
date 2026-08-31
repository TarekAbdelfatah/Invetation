using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services.Ideas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ibtikar.Services.Committee
{
    /// <summary>
    /// Daily hosted service that locks committee voting for ideas that have been
    /// open for voting (referred-committee status) for more than three days.
    /// Locked ideas transition to <c>under-assessment</c> so members can no
    /// longer cast or change votes. Runs on a 24-hour interval using the server
    /// clock, mirroring <see cref="Background.IdeaDeadlineHostedService"/>.
    /// </summary>
    public sealed class VoteLockHostedService : BackgroundService
    {
        private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(24);
        private static readonly TimeSpan VoteWindow = TimeSpan.FromDays(3);
        private const string AutoLockNote = "أُغلق باب التصويت تلقائياً بعد 3 أيام";

        private readonly IServiceProvider _services;
        private readonly ILogger<VoteLockHostedService> _logger;

        public VoteLockHostedService(
            IServiceProvider services,
            ILogger<VoteLockHostedService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    int locked = await LockExpiredVotesAsync(stoppingToken);
                    if (locked > 0)
                        _logger.LogInformation("Locked voting on {Count} ideas after 3-day window", locked);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Committee vote-lock sweep failed");
                }

                try
                {
                    await Task.Delay(SweepInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        private async Task<int> LockExpiredVotesAsync(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IbtikarDbContext>();

            var assessmentStatus = await db.IdeaStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Code == IdeaStatusCodes.UnderAssessment, ct);
            if (assessmentStatus is null)
            {
                _logger.LogWarning("IdeaStatusCodes.UnderAssessment is not seeded; vote-lock sweep skipped");
                return 0;
            }

            var cutoff = DateTime.UtcNow - VoteWindow;

            var candidates = await db.InnovationIdeas
                .AsNoTracking()
                .Include(i => i.StatusHistory)
                .Where(i => i.CurrentStatus != null
                    && i.CurrentStatus.Code == IdeaStatusCodes.ReferredCommittee)
                .ToListAsync(ct);

            var expiredIds = candidates
                .Where(i => LastTransitionAt(i) is DateTime at && at < cutoff)
                .Select(i => i.Id)
                .ToList();

            if (expiredIds.Count == 0) return 0;

            var expiredEntities = await db.InnovationIdeas
                .Where(i => expiredIds.Contains(i.Id))
                .ToListAsync(ct);

            var now = DateTime.UtcNow;
            foreach (var idea in expiredEntities)
            {
                var previousStatusId = idea.CurrentStatusId;
                idea.CurrentStatusId = assessmentStatus.Id;
                db.IdeaStatusHistories.Add(new IdeaStatusHistory
                {
                    InnovationIdeaId = idea.Id,
                    FromStatusId = previousStatusId,
                    ToStatusId = assessmentStatus.Id,
                    ChangedAt = now,
                    Note = AutoLockNote
                });
            }

            await db.SaveChangesAsync(ct);
            return expiredEntities.Count;
        }

        private static DateTime? LastTransitionAt(InnovationIdea idea)
        {
            if (idea.StatusHistory is null || idea.StatusHistory.Count == 0) return null;
            return idea.StatusHistory
                .Where(h => h.ToStatusId == idea.CurrentStatusId)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => (DateTime?)h.ChangedAt)
                .FirstOrDefault();
        }
    }
}
