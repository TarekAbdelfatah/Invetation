using Ibtikar.Data;
using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data.Seed
{
    public static class AssessmentCriterionSeed
    {
        public static void SeedCriteria(IbtikarDbContext db)
        {
            EnsureCriterion(db, 1, "strategic-alignment", "المواءمة الاستراتيجية", "مدى مساهمة الفكرة في تحقيق الأهداف الاستراتيجية");
            EnsureCriterion(db, 2, "impact-magnitude", "حجم الأثر", "مدى اتساع المستفيدين وحجم المنفعة المتوقعة");
            EnsureCriterion(db, 3, "feasibility", "الجدوى الفنية والتشغيلية", "مدى قابلية الفكرة للتنفيذ ضمن القدرات والإمكانات المتاحة");
            EnsureCriterion(db, 4, "cost-benefit", "العائد مقابل التكلفة", "مدى تناسب الجدوى المالية والاقتصادية مع الموارد المطلوبة");
            EnsureCriterion(db, 5, "innovation-level", "مستوى الابتكار", "مدى تميز الفكرة وحداثتها مقارنة بالممارسات القائمة");
        }

        private static void EnsureCriterion(IbtikarDbContext db, int displayOrder, string code, string name, string description)
        {
            var exists = db.AssessmentCriteria.Any(c => c.Code == code);
            if (exists) return;

            db.AssessmentCriteria.Add(new AssessmentCriterion
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                Description = description,
                DisplayOrder = displayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}