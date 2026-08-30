using Ibtikar.Data;
using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data.Seed
{
    public static class RoleSeed
    {
        public static void SeedRoles(IbtikarDbContext db)
        {
            EnsureRole(db, "external-beneficiary", "مستفيد خارجي", "External Beneficiary");
            EnsureRole(db, "internal-beneficiary", "مستفيد داخلي", "Internal Beneficiary");
            EnsureRole(db, "audit-employee", "موظف تدقيق", "Audit Employee");
            EnsureRole(db, "specialized-department", "الإدارة المختصة", "Specialized Department");
            EnsureRole(db, "partner-department", "الإدارة الشريكة", "Partner Department");
            EnsureRole(db, "innovation-committee-member", "عضو لجنة الابتكار", "Innovation Committee Member");
            EnsureRole(db, "system-admin", "مدير النظام", "System Admin");
        }

        private static void EnsureRole(IbtikarDbContext db, string code, string name, string? description)
        {
            var exists = db.Roles.Any(r => r.Code == code);
            if (exists) return;

            db.Roles.Add(new Role
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}