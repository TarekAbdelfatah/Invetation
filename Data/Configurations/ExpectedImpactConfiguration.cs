using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class ExpectedImpactConfiguration : IEntityTypeConfiguration<ExpectedImpact>
    {
        public void Configure(EntityTypeBuilder<ExpectedImpact> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Code).IsRequired().HasMaxLength(50);
            builder.HasIndex(e => e.Code).IsUnique();
            builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
            builder.Property(e => e.IsOther).IsRequired();
            builder.Property(e => e.DisplayOrder).IsRequired();
            builder.Property(e => e.IsActive).IsRequired();
            builder.Property(e => e.CreatedAt).IsRequired();
        }
    }
}