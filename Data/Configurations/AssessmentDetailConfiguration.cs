using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class AssessmentDetailConfiguration : IEntityTypeConfiguration<AssessmentDetail>
    {
        public void Configure(EntityTypeBuilder<AssessmentDetail> builder)
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Score).IsRequired();
            builder.Property(d => d.Comment).HasMaxLength(1000);

            builder.HasOne(d => d.Criterion)
                .WithMany()
                .HasForeignKey(d => d.CriterionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(d => d.AssessmentHeaderId);
        }
    }
}