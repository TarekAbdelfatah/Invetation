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
        public DbSet<ErpEmployee> Employees => Set<ErpEmployee>();
        public DbSet<ErpHrDepartment> HrDepartments => Set<ErpHrDepartment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CommonDepartment>(b =>
            {
                b.ToTable("Departments", "COMMON_SYS");
                b.HasKey(d => d.Id);
                b.Property(d => d.Name).IsRequired().HasMaxLength(200);
                b.Property(d => d.Code).HasMaxLength(50);
            });

            modelBuilder.Entity<ErpEmployee>(b =>
            {
                b.ToTable("Employees", "dbo");
                b.HasKey(e => e.NetworkUser);
                b.Property(e => e.NetworkUser).HasColumnName("NetworkUser").HasMaxLength(150);
                b.Property(e => e.Name).HasColumnName("Name").HasMaxLength(200);
            });

            modelBuilder.Entity<ErpHrDepartment>(b =>
            {
                b.ToTable("HR_DEPARTMENT", "COMMON_SYS");
                b.HasKey(d => d.DeptId);
                b.Property(d => d.DeptId).HasColumnName("DeptId");
                b.Property(d => d.DeptName).HasColumnName("DeptName").HasMaxLength(200);
            });
        }
    }
}
