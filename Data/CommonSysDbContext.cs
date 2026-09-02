using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data
{
    public class CommonSysDbContext : DbContext
    {
        public CommonSysDbContext(DbContextOptions<CommonSysDbContext> options)
            : base(options)
        {
        }

        public DbSet<CommonDepartment> Departments => Set<CommonDepartment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CommonDepartment>(b =>
            {
                b.ToTable("Departments");
                b.HasKey(d => d.Id);
                b.Property(d => d.Name).IsRequired().HasMaxLength(200);
                b.Property(d => d.Code).HasMaxLength(50);
            });
        }
    }
}
