using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
            builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
            builder.HasIndex(r => r.Code).IsUnique();
            builder.Property(r => r.Description).HasMaxLength(500);
            builder.Property(r => r.IsActive).IsRequired();
            builder.Property(r => r.CreatedAt).IsRequired();
        }
    }
}