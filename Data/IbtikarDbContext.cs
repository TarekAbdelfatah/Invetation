using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data
{
    public class IbtikarDbContext : DbContext
    {
        public IbtikarDbContext(DbContextOptions<IbtikarDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserTypeLookup> UserTypes => Set<UserTypeLookup>();
        public DbSet<User> Users => Set<User>();
        public DbSet<IdeaStatus> IdeaStatuses => Set<IdeaStatus>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<AssessmentCriterion> AssessmentCriteria => Set<AssessmentCriterion>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(IbtikarDbContext).Assembly);
        }
    }
}