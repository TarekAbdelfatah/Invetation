using Ibtikar.Data;
using Ibtikar.Models;

namespace Ibtikar.Data.Seed
{
    public static class RoleSeed
    {
        public static void SeedRoles(IbtikarDbContext db)
        {
            EnsureRole(db, "AuditEmployee", "موظف تدقيق", "Audit Employee");
            EnsureRole(db, "SpecializedDepartment", "الإدارة المختصة", "Specialized Department");
            EnsureRole(db, "InnovationCommitteeMember", "عضو لجنة الابتكار", "Innovation Committee Member");
            EnsureRole(db, "InnovationCommitteeHead", "رئيس لجنة الابتكار", "Innovation Committee Head");
            EnsureRole(db, "admin", "مدير النظام", "Admin");
            
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
