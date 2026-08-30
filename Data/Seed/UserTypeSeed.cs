using Ibtikar.Data;
using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data.Seed
{
    public static class UserTypeSeed
    {
        public static void SeedUserTypes(IbtikarDbContext db)
        {
            EnsureType(db, 1, "internal", "داخلي", "Internal");
            EnsureType(db, 2, "external", "خارجي", "External");
        }

        private static void EnsureType(IbtikarDbContext db, int displayOrder, string code, string name, string nameEn)
        {
            var exists = db.UserTypes.Any(t => t.Code == code);
            if (exists) return;

            db.UserTypes.Add(new UserTypeLookup
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                DisplayOrder = displayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}