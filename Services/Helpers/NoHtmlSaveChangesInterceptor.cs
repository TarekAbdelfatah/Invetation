using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ibtikar.Services.Helpers
{
    public sealed class NoHtmlSaveChangesInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            CleanTrackedStrings(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CleanTrackedStrings(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static void CleanTrackedStrings(DbContext? context)
        {
            if (context is null)
            {
                return;
            }

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified))
                {
                    continue;
                }

                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.ClrType != typeof(string))
                    {
                        continue;
                    }

                    if (property.CurrentValue is not string raw)
                    {
                        continue;
                    }

                    var cleaned = SafeText.Clean(raw);
                    if (!string.Equals(cleaned, raw, StringComparison.Ordinal))
                    {
                        property.CurrentValue = cleaned;
                    }
                }
            }
        }
    }
}