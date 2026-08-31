using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services.Ideas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ibtikar.Services.Background
{
    /// <summary>
    /// Daily hosted service that closes ideas stuck on the applicant in
    /// <c>waiting_for_completion</c> or <c>returned_for_development</c> for more
    /// than 14 days. Stale ideas transition to <c>cancelled</c> and an audit
    /// row is appended to <see cref="IdeaStatusHistory"/> so the timeline stays
    /// honest. Runs on a 24-hour interval using the server clock.
    /// </summary>
    public sealed class IdeaDeadlineHostedService : BackgroundService
    {
        private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(24);
        private static readonly TimeSpan StaleAfter = TimeSpan.FromDays(14);
        private const string AutoCloseNote = "انتهت المهلة المتاحة للتعديل (14 يوم)";

        private readonly IServiceProvider _services;
        private readonly ILogger<IdeaDeadlineHostedService> _logger;

        public IdeaDeadlineHostedService(
            IServiceProvider services,
            ILogger<IdeaDeadlineHostedService> logger)
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
                    int closed = await CloseStaleIdeasAsync(stoppingToken);
                    if (closed > 0)
                        _logger.LogInformation("Closed {Count} stale ideas after deadline sweep", closed);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Idea deadline sweep failed");
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

        private async Task<int> CloseStaleIdeasAsync(CancellationToken ct)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IbtikarDbContext>();

            var cancelledStatus = await db.IdeaStatuses
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Code == IdeaStatusCodes.Cancelled, ct);
            if (cancelledStatus is null)
            {
                _logger.LogWarning("IdeaStatusCodes.Cancelled is not seeded; deadline sweep skipped");
                return 0;
            }

            var cutoff = DateTime.UtcNow - StaleAfter;

            var candidates = await db.InnovationIdeas
                .AsNoTracking()
                .Include(i => i.StatusHistory)
                .Where(i => i.CurrentStatus != null
                    && (i.CurrentStatus.Code == IdeaStatusCodes.WaitingForCompletion
                        || i.CurrentStatus.Code == IdeaStatusCodes.ReturnedForDevelopment))
                .ToListAsync(ct);

            var staleIds = candidates
                .Where(i => LastTransitionAt(i) is DateTime at && at < cutoff)
                .Select(i => i.Id)
                .ToList();

            if (staleIds.Count == 0) return 0;

            var staleEntities = await db.InnovationIdeas
                .Where(i => staleIds.Contains(i.Id))
                .ToListAsync(ct);

            var now = DateTime.UtcNow;
            foreach (var idea in staleEntities)
            {
                var previousStatusId = idea.CurrentStatusId;
                idea.CurrentStatusId = cancelledStatus.Id;
                db.IdeaStatusHistories.Add(new IdeaStatusHistory
                {
                    InnovationIdeaId = idea.Id,
                    FromStatusId = previousStatusId,
                    ToStatusId = cancelledStatus.Id,
                    ChangedAt = now,
                    Note = AutoCloseNote
                });
            }

            await db.SaveChangesAsync(ct);
            return staleEntities.Count;
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
