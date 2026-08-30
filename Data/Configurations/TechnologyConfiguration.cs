using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class TechnologyConfiguration : IEntityTypeConfiguration<Technology>
    {
        public void Configure(EntityTypeBuilder<Technology> builder)
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Code).IsRequired().HasMaxLength(50);
            builder.HasIndex(t => t.Code).IsUnique();
            builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
            builder.Property(t => t.IsOther).IsRequired();
            builder.Property(t => t.DisplayOrder).IsRequired();
            builder.Property(t => t.IsActive).IsRequired();
            builder.Property(t => t.CreatedAt).IsRequired();
        }
    }
}