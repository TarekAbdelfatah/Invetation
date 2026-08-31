using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class IdeaStatusHistoryConfiguration : IEntityTypeConfiguration<IdeaStatusHistory>
    {
        public void Configure(EntityTypeBuilder<IdeaStatusHistory> builder)
        {
            builder.HasKey(h => h.Id);
            builder.Property(h => h.ChangedAt).IsRequired();
            builder.Property(h => h.Note).HasMaxLength(500);

            builder.HasOne(h => h.InnovationIdea)
                .WithMany(i => i.StatusHistory)
                .HasForeignKey(h => h.InnovationIdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(h => h.FromStatus)
                .WithMany()
                .HasForeignKey(h => h.FromStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(h => h.ToStatus)
                .WithMany()
                .HasForeignKey(h => h.ToStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(h => h.ChangedBy)
                .WithMany()
                .HasForeignKey(h => h.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
