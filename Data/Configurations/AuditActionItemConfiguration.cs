using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class AuditActionItemConfiguration : IEntityTypeConfiguration<AuditActionItem>
    {
        public void Configure(EntityTypeBuilder<AuditActionItem> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Decision).IsRequired().HasMaxLength(50);
            builder.Property(a => a.DecisionText).HasMaxLength(2000);
            builder.Property(a => a.AuditDate).IsRequired();

            builder.HasOne(a => a.Idea)
                .WithMany(i => i.AuditActions)
                .HasForeignKey(a => a.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.TargetDepartment)
                .WithMany()
                .HasForeignKey(a => a.TargetDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(a => a.Auditor)
                .WithMany()
                .HasForeignKey(a => a.AuditorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => a.IdeaId);
        }
    }
}
