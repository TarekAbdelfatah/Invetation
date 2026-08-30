using Ibtikar.Data;
using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data.Seed
{
    public static class CriterionScoringSeed
    {
        public static void SeedScoring(IbtikarDbContext db)
        {
            EnsureScore(db, 1, 20);
            EnsureScore(db, 2, 40);
            EnsureScore(db, 3, 60);
            EnsureScore(db, 4, 80);
            EnsureScore(db, 5, 100);
        }

        private static void EnsureScore(IbtikarDbContext db, int score, int percent)
        {
            var exists = db.CriterionScorings.Any(s => s.Score == score);
            if (exists) return;

            db.CriterionScorings.Add(new CriterionScoring
            {
                Id = Guid.NewGuid(),
                Score = score,
                Percent = percent,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}