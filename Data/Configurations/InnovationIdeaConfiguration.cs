using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class InnovationIdeaConfiguration : IEntityTypeConfiguration<InnovationIdea>
    {
        public void Configure(EntityTypeBuilder<InnovationIdea> builder)
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.ReferenceNumber).IsRequired().HasMaxLength(30);
            builder.HasIndex(i => i.ReferenceNumber).IsUnique().HasFilter("\"IsDraft\" = false");
            builder.Property(i => i.Title).IsRequired().HasMaxLength(300);
            builder.Property(i => i.Description).IsRequired().HasMaxLength(4000);
            builder.Property(i => i.ProblemStatement).HasMaxLength(4000);
            builder.Property(i => i.ProposedSolution).HasMaxLength(4000);
            builder.Property(i => i.ExpectedBenefits).HasMaxLength(4000);
            builder.Property(i => i.IsDraft).IsRequired();
            builder.HasIndex(i => i.ApplicantUserId).HasFilter("\"IsDeleted\" = false");
            builder.Property(i => i.IsDeleted).IsRequired();
            builder.Property(i => i.DeletedAt);
            builder.Property(i => i.CreatedAt).IsRequired();

            builder.HasOne(i => i.InnovationDomain)
                .WithMany()
                .HasForeignKey(i => i.InnovationDomainId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.ExpectedImpact)
                .WithMany()
                .HasForeignKey(i => i.ExpectedImpactId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(i => i.TargetAudience)
                .WithMany()
                .HasForeignKey(i => i.TargetAudienceId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(i => i.CurrentStatus)
                .WithMany()
                .HasForeignKey(i => i.CurrentStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.ApplicantUser)
                .WithMany()
                .HasForeignKey(i => i.ApplicantUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(i => i.ApplicantDepartment)
                .WithMany()
                .HasForeignKey(i => i.ApplicantDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(i => i.AssignedDepartment)
                .WithMany()
                .HasForeignKey(i => i.AssignedDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}