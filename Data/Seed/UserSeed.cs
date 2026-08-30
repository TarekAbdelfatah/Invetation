using Ibtikar.Data;
using Ibtikar.Models;
using Ibtikar.Services;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data.Seed
{
    public static class UserSeed
    {
        public const string DefaultPassword = "Ibtikar@2026";

        public static void SeedTestUsers(IbtikarDbContext db, Pbkdf2PasswordHasher hasher)
        {
            EnsureUserForRole(db, hasher, "audit", "audit", "موظف تدقيق", "audit-employee", "internal");
            EnsureUserForRole(db, hasher, "specialized", "specialized", "الإدارة المختصة", "specialized-department", "internal");
            EnsureUserForRole(db, hasher, "partner", "partner", "الإدارة الشريكة", "partner-department", "internal");
            EnsureUserForRole(db, hasher, "committee", "committee", "عضو لجنة", "innovation-committee-member", "internal");
            EnsureUserForRole(db, hasher, "admin", "admin", "مدير النظام", "system-admin", "internal");
            EnsureUserForRole(db, hasher, "ext-beneficiary", "ext", "مستفيد خارجي", "external-beneficiary", "external");
            EnsureUserForRole(db, hasher, "int-beneficiary", "int", "مستفيد داخلي", "internal-beneficiary", "internal");
        }

        private static void EnsureUserForRole(IbtikarDbContext db, Pbkdf2PasswordHasher hasher, string username, string emailLocal, string fullName, string roleCode, string userTypeCode)
        {
            if (db.Users.Any(u => u.Username == username)) return;

            var role = db.Roles.FirstOrDefault(r => r.Code == roleCode);
            if (role == null) return;

            var userType = db.UserTypes.FirstOrDefault(t => t.Code == userTypeCode);
            if (userType == null) return;

            var hashResult = hasher.Hash(DefaultPassword);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                FullName = fullName,
                Email = $"{emailLocal}@ibtikar.local",
                PasswordHash = hashResult.Hash,
                PasswordSalt = hashResult.Salt,
                UserTypeId = userType.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            db.SaveChanges();

            db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }
    }
}