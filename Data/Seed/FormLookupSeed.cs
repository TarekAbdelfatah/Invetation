using Ibtikar.Data;
using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data.Seed
{
    public static class FormLookupSeed
    {
        public static void SeedFormLookups(IbtikarDbContext db)
        {
            EnsureImpact(db, "financial", "مالي", false, 1);
            EnsureImpact(db, "operational", "تشغيلي", false, 2);
            EnsureImpact(db, "strategic", "استراتيجي", false, 3);
            EnsureImpact(db, "other", "أخرى", true, 99);

            EnsureAudience(db, "other", "أخرى", true, 1);

            EnsureTechnology(db, "ai", "الذكاء الاصطناعي", false, 1);
            EnsureTechnology(db, "iot", "إنترنت الأشياء", false, 2);
            EnsureTechnology(db, "blockchain", "بلوكتشين", false, 3);
            EnsureTechnology(db, "cloud", "الحوسبة السحابية", false, 4);
            EnsureTechnology(db, "bigdata", "تحليل البيانات الضخمة", false, 5);
            EnsureTechnology(db, "ar_vr", "الواقع المعزز والافتراضي", false, 6);
            EnsureTechnology(db, "rpa", "الأتمتة الروبوتية", false, 7);
            EnsureTechnology(db, "5g", "شبكات الجيل الخامس", false, 8);
            EnsureTechnology(db, "cybersecurity", "الأمن السيبراني", false, 9);
            EnsureTechnology(db, "other", "أخرى", true, 99);

            EnsureExecutionStage(db, 1, "initiation", "البدء");
            EnsureExecutionStage(db, 2, "planning", "التخطيط");
            EnsureExecutionStage(db, 3, "execution", "التنفيذ");
            EnsureExecutionStage(db, 4, "monitoring", "المتابعة");
            EnsureExecutionStage(db, 5, "closure", "الإغلاق");
        }

        private static void EnsureImpact(IbtikarDbContext db, string code, string name, bool isOther, int order)
        {
            if (db.ExpectedImpacts.Any(e => e.Code == code)) return;
            db.ExpectedImpacts.Add(new ExpectedImpact
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                IsOther = isOther,
                DisplayOrder = order,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        private static void EnsureAudience(IbtikarDbContext db, string code, string name, bool isOther, int order)
        {
            if (db.TargetAudiences.Any(t => t.Code == code)) return;
            db.TargetAudiences.Add(new TargetAudience
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                IsOther = isOther,
                DisplayOrder = order,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        private static void EnsureTechnology(IbtikarDbContext db, string code, string name, bool isOther, int order)
        {
            if (db.Technologies.Any(t => t.Code == code)) return;
            db.Technologies.Add(new Technology
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                IsOther = isOther,
                DisplayOrder = order,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        private static void EnsureExecutionStage(IbtikarDbContext db, int order, string code, string name)
        {
            if (db.ExecutionStages.Any(s => s.Order == order)) return;
            db.ExecutionStages.Add(new ExecutionStage
            {
                Id = Guid.NewGuid(),
                Order = order,
                Code = code,
                Name = name,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}