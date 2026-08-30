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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(IbtikarDbContext).Assembly);
        }
    }
}