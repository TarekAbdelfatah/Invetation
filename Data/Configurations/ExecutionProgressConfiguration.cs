using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class ExecutionProgressConfiguration : IEntityTypeConfiguration<ExecutionProgress>
    {
        public void Configure(EntityTypeBuilder<ExecutionProgress> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Note).IsRequired().HasMaxLength(500);
            builder.Property(p => p.ChangedAt).IsRequired();
            builder.HasIndex(p => p.InnovationIdeaId);
            builder.HasIndex(p => new { p.InnovationIdeaId, p.ChangedAt });

            builder.HasOne(p => p.InnovationIdea)
                .WithMany()
                .HasForeignKey(p => p.InnovationIdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.ExecutionStage)
                .WithMany()
                .HasForeignKey(p => p.ExecutionStageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.ChangedBy)
                .WithMany()
                .HasForeignKey(p => p.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
