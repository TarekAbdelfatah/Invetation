using Ibtikar.Data;
using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data.Seed
{
    public static class InnovationDomainSeed
    {
        public static void SeedInnovationDomains(IbtikarDbContext db)
        {
            EnsureDomain(db, 1, "judicial-services", "خدمات قضائية", "Judicial Services");
            EnsureDomain(db, 2, "administrative-procedures", "إجراءات إدارية", "Administrative Procedures");
            EnsureDomain(db, 3, "digital-transformation", "تحول رقمي", "Digital Transformation");
        }

        private static void EnsureDomain(IbtikarDbContext db, int displayOrder, string code, string name, string nameEn)
        {
            var exists = db.InnovationDomains.Any(d => d.Code == code);
            if (exists) return;

            db.InnovationDomains.Add(new InnovationDomain
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