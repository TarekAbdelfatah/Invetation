using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class PartnerAssignmentConfiguration : IEntityTypeConfiguration<PartnerAssignment>
    {
        public void Configure(EntityTypeBuilder<PartnerAssignment> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Status).IsRequired().HasMaxLength(20);
            builder.Property(p => p.Note).HasMaxLength(2000);
            builder.Property(p => p.SentAt).IsRequired();

            builder.HasOne(p => p.InnovationIdea)
                .WithMany()
                .HasForeignKey(p => p.InnovationIdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.PartnerDepartment)
                .WithMany()
                .HasForeignKey(p => p.PartnerDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.RequestedBy)
                .WithMany()
                .HasForeignKey(p => p.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => p.InnovationIdeaId);
            builder.HasIndex(p => p.PartnerDepartmentId);
            builder.HasIndex(p => new { p.InnovationIdeaId, p.PartnerDepartmentId }).IsUnique();
        }
    }
}