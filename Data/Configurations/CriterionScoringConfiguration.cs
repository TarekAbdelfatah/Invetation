using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class CriterionScoringConfiguration : IEntityTypeConfiguration<CriterionScoring>
    {
        public void Configure(EntityTypeBuilder<CriterionScoring> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Score).IsRequired();
            builder.HasIndex(s => s.Score).IsUnique();
            builder.Property(s => s.Percent).IsRequired();
            builder.Property(s => s.CreatedAt).IsRequired();
        }
    }
}