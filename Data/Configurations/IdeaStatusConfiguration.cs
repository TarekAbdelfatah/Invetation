using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class IdeaStatusConfiguration : IEntityTypeConfiguration<IdeaStatus>
    {
        public void Configure(EntityTypeBuilder<IdeaStatus> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
            builder.HasIndex(s => s.Code).IsUnique();
            builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
            builder.Property(s => s.NameEn).HasMaxLength(100);
            builder.Property(s => s.Color).IsRequired().HasMaxLength(20);
            builder.Property(s => s.DisplayOrder).IsRequired();
            builder.Property(s => s.IsActive).IsRequired();
            builder.Property(s => s.IsTerminal).IsRequired();
            builder.Property(s => s.CreatedAt).IsRequired();
        }
    }
}