using Ibtikar.Data;
using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data.Seed
{
    public static class DepartmentSeed
    {
        public static void SeedDepartments(IbtikarDbContext db)
        {
            EnsureDepartment(db, "tech", "التقنية", "Technology");
            EnsureDepartment(db, "infrastructure", "البنية التحتية", "Infrastructure");
        }

        private static void EnsureDepartment(IbtikarDbContext db, string code, string name, string? nameEn)
        {
            var exists = db.Departments.Any(d => d.Code == code);
            if (exists) return;

            db.Departments.Add(new Department
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                NameEn = nameEn,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}