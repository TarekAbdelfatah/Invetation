using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services.Ideas;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data.Seed
{
    public static class IdeaStatusSeed
    {
        public static void SeedIdeaStatuses(IbtikarDbContext db)
        {
            EnsureStatus(db, 1, IdeaStatusCodes.New, "جديد", "New", "#0d6efd", false);
            EnsureStatus(db, 2, IdeaStatusCodes.UnderReview, "تحت الدراسة", "Under Review", "#ffc107", false);
            EnsureStatus(db, 3, IdeaStatusCodes.ReferredCommittee, "محول للجنة", "Referred to Committee", "#6f42c1", false);
            EnsureStatus(db, 4, IdeaStatusCodes.UnderAssessment, "قيد التقييم", "Under Assessment", "#0dcaf0", false);
            EnsureStatus(db, 5, IdeaStatusCodes.Approved, "معتمد", "Approved", "#198754", true);
            EnsureStatus(db, 6, IdeaStatusCodes.Rejected, "مرفوض", "Rejected", "#dc3545", true);
            EnsureStatus(db, 7, IdeaStatusCodes.Deferred, "مؤجل", "Deferred", "#fd7e14", false);
            EnsureStatus(db, 8, IdeaStatusCodes.InExecution, "قيد التنفيذ", "In Execution", "#20c997", false);
            EnsureStatus(db, 9, IdeaStatusCodes.Completed, "منجز", "Completed", "#198754", true);
            EnsureStatus(db, 10, IdeaStatusCodes.Cancelled, "ملغي", "Cancelled", "#6c757d", true);
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