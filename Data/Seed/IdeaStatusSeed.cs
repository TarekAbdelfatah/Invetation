using Ibtikar.Data;
using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data.Seed
{
    public static class IdeaStatusSeed
    {
        public static void SeedIdeaStatuses(IbtikarDbContext db)
        {
            EnsureStatus(db, 1, "new", "جديد", "New", "#0d6efd", false);
            EnsureStatus(db, 2, "under-review", "تحت الدراسة", "Under Review", "#ffc107", false);
            EnsureStatus(db, 3, "referred-committee", "محول للجنة", "Referred to Committee", "#6f42c1", false);
            EnsureStatus(db, 4, "under-assessment", "قيد التقييم", "Under Assessment", "#0dcaf0", false);
            EnsureStatus(db, 5, "approved", "معتمد", "Approved", "#198754", true);
            EnsureStatus(db, 6, "rejected", "مرفوض", "Rejected", "#dc3545", true);
            EnsureStatus(db, 7, "deferred", "مؤجل", "Deferred", "#fd7e14", false);
            EnsureStatus(db, 8, "in-execution", "قيد التنفيذ", "In Execution", "#20c997", false);
            EnsureStatus(db, 9, "completed", "منجز", "Completed", "#198754", true);
            EnsureStatus(db, 10, "cancelled", "ملغي", "Cancelled", "#6c757d", true);
        }

        private static void EnsureStatus(IbtikarDbContext db, int displayOrder, string code, string name, string nameEn, string color, bool isTerminal)
        {
            var exists = db.IdeaStatuses.Any(s => s.Code == code);
            if (exists) return;

            db.IdeaStatuses.Add(new IdeaStatus
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                NameEn = nameEn,
                Color = color,
                DisplayOrder = displayOrder,
                IsActive = true,
                IsTerminal = isTerminal,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}