using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data.Seed
{
    public static class InnovationIdeaSeed
    {
        public static void SeedSampleIdeas(IbtikarDbContext db)
        {
            var domains = db.InnovationDomains.OrderBy(d => d.DisplayOrder).ToList();
            var statuses = db.IdeaStatuses.ToList();
            if (domains.Count == 0 || statuses.Count == 0) return;

            var applicantUserId = EnsurePlaceholderApplicant(db);
            var judicialDeptId = db.Departments.FirstOrDefault(d => d.Code == "judicial")?.Id;
            var techDeptId = db.Departments.FirstOrDefault(d => d.Code == "tech")?.Id;
            var partnerDeptId = db.Departments.FirstOrDefault(d => d.Code == "infrastructure")?.Id;

            var newStatus = statuses.First(s => s.Code == IdeaStatusCodes.New);
            var underReview = statuses.First(s => s.Code == IdeaStatusCodes.UnderReview);
            var underStudy = statuses.First(s => s.Code == IdeaStatusCodes.UnderStudy);
            var inExecution = statuses.First(s => s.Code == IdeaStatusCodes.InExecution);
            var rejected = statuses.First(s => s.Code == IdeaStatusCodes.Rejected);
            var referred = statuses.First(s => s.Code == IdeaStatusCodes.ReferredCommittee);
            var approved = statuses.First(s => s.Code == IdeaStatusCodes.Approved);

            var judicialDomain = domains.FirstOrDefault(d => d.Code == "judicial-services");
            var adminDomain = domains.FirstOrDefault(d => d.Code == "administrative-procedures");
            var digitalDomain = domains.FirstOrDefault(d => d.Code == "digital-transformation");

            if (!db.InnovationIdeas.Any(i => i.ReferenceNumber == "IBT-2026-0001"))
            {
                db.InnovationIdeas.Add(new InnovationIdea
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
                });
            }

            if (!db.InnovationIdeas.Any(i => i.ReferenceNumber == "IBT-2026-0002"))
            {
                db.InnovationIdeas.Add(new InnovationIdea
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
                });
            }

            if (!db.InnovationIdeas.Any(i => i.ReferenceNumber == "IBT-2026-0003"))
            {
                db.InnovationIdeas.Add(new InnovationIdea
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
                });
            }

            if (!db.InnovationIdeas.Any(i => i.ReferenceNumber == "IBT-2026-0004"))
            {
                db.InnovationIdeas.Add(new InnovationIdea
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
                });
            }

            if (judicialDeptId.HasValue && !db.InnovationIdeas.Any(i => i.ReferenceNumber == "IBT-2026-1001"))
            {
                db.InnovationIdeas.Add(new InnovationIdea
                {
                    Id = Guid.NewGuid(),
                    ReferenceNumber = "IBT-2026-1001",
                    Title = "تقييم فني لقاعدة بيانات القضايا المركزية",
                    Description = "مراجعة شاملة لأداء قواعد البيانات وتحديد فرص التحسين قبل بدء موسم الذروة القضائية القادم.",
                    ProblemStatement = "تباطؤ ملحوظ في استعلامات القضايا التراكمية خلال ساعات الذروة.",
                    ProposedSolution = "ترحيل الأقسام الحرجة إلى فهارس عمودية ومراجعة خطة الفهرسة الحالية.",
                    ExpectedBenefits = "تقليل زمن الاستعلام من 12 ثانية إلى أقل من ثانيتين في 90% من الاستعلامات.",
                    InnovationDomainId = digitalDomain?.Id ?? domains[0].Id,
                    CurrentStatusId = underStudy.Id,
                    ApplicantUserId = applicantUserId,
                    ApplicantDepartmentId = judicialDeptId,
                    AssignedDepartmentId = judicialDeptId,
                    IsDraft = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    SubmittedAt = DateTime.UtcNow.AddDays(-4),
                    AuditAssignedAt = DateTime.UtcNow.AddDays(-3)
                });
            }

            if (judicialDeptId.HasValue && !db.InnovationIdeas.Any(i => i.ReferenceNumber == "IBT-2026-1002"))
            {
                db.InnovationIdeas.Add(new InnovationIdea
                {
                    Id = Guid.NewGuid(),
                    ReferenceNumber = "IBT-2026-1002",
                    Title = "تطبيق الإحالات الإلكترونية بين المحاكم",
                    Description = "نظام إلكتروني موحد لإحالة القضايا بين المحاكم الابتدائية والاستئنافية دون الحاجة للمراسلات الورقية.",
                    InnovationDomainId = judicialDomain?.Id ?? domains[0].Id,
                    CurrentStatusId = underStudy.Id,
                    ApplicantUserId = applicantUserId,
                    ApplicantDepartmentId = judicialDeptId,
                    AssignedDepartmentId = judicialDeptId,
                    IsDraft = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-9),
                    SubmittedAt = DateTime.UtcNow.AddDays(-8),
                    AuditAssignedAt = DateTime.UtcNow.AddDays(-5)
                });
            }

            if (judicialDeptId.HasValue && !db.InnovationIdeas.Any(i => i.ReferenceNumber == "IBT-2026-1003"))
            {
                db.InnovationIdeas.Add(new InnovationIdea
                {
                    Id = Guid.NewGuid(),
                    ReferenceNumber = "IBT-2026-1003",
                    Title = "خدمة الاستعلام الصوتي عن مواعيد الجلسات",
                    Description = "استعلام صوتي عبر الهاتف لمعرفة موعد الجلسة القادمة وحالة الملف التنفيذي.",
                    InnovationDomainId = digitalDomain?.Id ?? domains[0].Id,
                    CurrentStatusId = inExecution.Id,
                    ApplicantUserId = applicantUserId,
                    ApplicantDepartmentId = judicialDeptId,
                    AssignedDepartmentId = judicialDeptId,
                    IsDraft = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-40),
                    SubmittedAt = DateTime.UtcNow.AddDays(-38),
                    AuditAssignedAt = DateTime.UtcNow.AddDays(-35)
                });
            }

            if (judicialDeptId.HasValue && !db.InnovationIdeas.Any(i => i.ReferenceNumber == "IBT-2026-1004"))
            {
                db.InnovationIdeas.Add(new InnovationIdea
                {
                    Id = Guid.NewGuid(),
                    ReferenceNumber = "IBT-2026-1004",
                    Title = "مكتبة رقمية للاجتهادات القضائية",
                    Description = "تجميع الاجتهادات القضائية القديمة والحديثة في قاعدة بيانات قابلة للبحث الدلالي.",
                    InnovationDomainId = judicialDomain?.Id ?? domains[0].Id,
                    CurrentStatusId = rejected.Id,
                    ApplicantUserId = applicantUserId,
                    ApplicantDepartmentId = judicialDeptId,
                    AssignedDepartmentId = judicialDeptId,
                    IsDraft = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    SubmittedAt = DateTime.UtcNow.AddDays(-23),
                    AuditAssignedAt = DateTime.UtcNow.AddDays(-20)
                });
            }

            db.SaveChanges();
        }

        private static Guid EnsurePlaceholderApplicant(IbtikarDbContext db)
        {
            var existing = db.Users.FirstOrDefault(u => u.Username == "system-applicant");
            if (existing != null) return existing.Id;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "system-applicant",
                FullName = "مقدّم نموذجي",
                Email = "system-applicant@ibtikar.local",
                PasswordHash = "placeholder-not-usable",
                PasswordSalt = "placeholder",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            db.SaveChanges();
            return user.Id;
        }
    }
}