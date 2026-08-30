using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
            builder.Property(d => d.NameEn).HasMaxLength(200);
            builder.Property(d => d.Code).IsRequired().HasMaxLength(50);
            builder.HasIndex(d => d.Code).IsUnique();
            builder.Property(d => d.IsActive).IsRequired();
            builder.Property(d => d.CreatedAt).IsRequired();
        }
    }
}