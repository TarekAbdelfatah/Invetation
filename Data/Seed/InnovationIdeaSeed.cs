using Ibtikar.Data;
using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data.Seed
{
    public static class InnovationIdeaSeed
    {
        public static void SeedSampleIdeas(IbtikarDbContext db)
        {
            if (db.InnovationIdeas.Any()) return;

            var domains = db.InnovationDomains.OrderBy(d => d.DisplayOrder).ToList();
            var statuses = db.IdeaStatuses.ToList();
            if (domains.Count == 0 || statuses.Count == 0) return;

            var applicantUserId = EnsurePlaceholderApplicant(db);

            var judicialDomain = domains.FirstOrDefault(d => d.Code == "judicial-services");
            var adminDomain = domains.FirstOrDefault(d => d.Code == "administrative-procedures");
            var digitalDomain = domains.FirstOrDefault(d => d.Code == "digital-transformation");

            var newStatus = statuses.First(s => s.Code == "new");
            var underReview = statuses.First(s => s.Code == "under-review");
            var referred = statuses.First(s => s.Code == "referred-committee");
            var approved = statuses.First(s => s.Code == "approved");

            var ideas = new List<InnovationIdea>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ReferenceNumber = "IBT-2026-0001",
                    Title = "منصة المواعيد الإلكترونية الموحدة لقاعات المحاكم",
                    Description = "نظام يتيح لأطراف الدعوى حجز موعد مسبق لمراجعة القاضي عبر البوابة الإلكترونية مع تنبيه آلي قبل الموعد.",
                    InnovationDomainId = judicialDomain?.Id ?? domains[0].Id,
                    CurrentStatusId = newStatus.Id,
                    ApplicantUserId = applicantUserId,
                    IsDraft = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-12),
                    SubmittedAt = DateTime.UtcNow.AddDays(-12)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ReferenceNumber = "IBT-2026-0002",
                    Title = "أرشفة ذكية لملفات التنفيذ الجبري",
                    Description = "حل يعتمد على التعرف الضوئي على الأحكام لفهرسة ملفات التنفيذ وتمكين البحث اللحظي بدل الجرد اليدوي.",
                    InnovationDomainId = adminDomain?.Id ?? domains[0].Id,
                    CurrentStatusId = underReview.Id,
                    ApplicantUserId = applicantUserId,
                    IsDraft = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    SubmittedAt = DateTime.UtcNow.AddDays(-7)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ReferenceNumber = "IBT-2026-0003",
                    Title = "لوحة متابعة أداء المحاكم في الزمن الحقيقي",
                    Description = "لوحة بيانات حية تعرض متوسط زمن الفصل، نسبة الإنجاز، وأعداد الجلسات لكل محكمة مع تنبيهات الانحراف.",
                    InnovationDomainId = digitalDomain?.Id ?? domains[0].Id,
                    CurrentStatusId = referred.Id,
                    ApplicantUserId = applicantUserId,
                    IsDraft = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    SubmittedAt = DateTime.UtcNow.AddDays(-3)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ReferenceNumber = "IBT-2026-0004",
                    Title = "إخطار الأطراف بالقرارات عبر الرسائل النصية",
                    Description = "خدمة ترسل نص الحكم وملخصه إلى أطراف الدعوى فور صدوره مع رابط للاطلاع على النسخة الكاملة.",
                    InnovationDomainId = digitalDomain?.Id ?? domains[0].Id,
                    CurrentStatusId = approved.Id,
                    ApplicantUserId = applicantUserId,
                    IsDraft = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    SubmittedAt = DateTime.UtcNow.AddDays(-30)
                }
            };

            db.InnovationIdeas.AddRange(ideas);
        }

        private static Guid EnsurePlaceholderApplicant(IbtikarDbContext db)
        {
            var existing = db.Users.FirstOrDefault(u => u.Username == "system-applicant");
            if (existing != null) return existing.Id;

            var externalUserType = db.UserTypes.FirstOrDefault(t => t.Code == "external");
            var userTypeId = externalUserType?.Id ?? db.UserTypes.First().Id;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "system-applicant",
                FullName = "مقدّم نموذجي",
                Email = "system-applicant@ibtikar.local",
                PasswordHash = "placeholder-not-usable",
                PasswordSalt = "placeholder",
                UserTypeId = userTypeId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            db.SaveChanges();
            return user.Id;
        }
    }
}