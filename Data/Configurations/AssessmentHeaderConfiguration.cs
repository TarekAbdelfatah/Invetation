using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class AssessmentHeaderConfiguration : IEntityTypeConfiguration<AssessmentHeader>
    {
        public void Configure(EntityTypeBuilder<AssessmentHeader> builder)
        {
            builder.HasKey(h => h.Id);
            builder.Property(h => h.Source).IsRequired().HasMaxLength(20);
            builder.Property(h => h.Comment).HasMaxLength(2000);
            builder.Property(h => h.CreatedAt).IsRequired();

            builder.HasOne(h => h.InnovationIdea)
                .WithMany()
                .HasForeignKey(h => h.InnovationIdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(h => h.Assessor)
                .WithMany()
                .HasForeignKey(h => h.AssessorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(h => h.AssessorDepartment)
                .WithMany()
                .HasForeignKey(h => h.AssessorDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(h => h.Details)
                .WithOne(d => d.AssessmentHeader)
                .HasForeignKey(d => d.AssessmentHeaderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(h => h.InnovationIdeaId);
            builder.HasIndex(h => new { h.InnovationIdeaId, h.Source });
        }
    }
}