using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class CommitteeDelegationConfiguration : IEntityTypeConfiguration<CommitteeDelegation>
    {
        public void Configure(EntityTypeBuilder<CommitteeDelegation> builder)
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.StartAt).IsRequired();
            builder.Property(d => d.EndAt).IsRequired();
            builder.Property(d => d.CreatedAt).IsRequired();

            builder.HasOne(d => d.InnovationCommittee)
                .WithMany()
                .HasForeignKey(d => d.InnovationCommitteeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.Head)
                .WithMany()
                .HasForeignKey(d => d.HeadUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.DelegateMember)
                .WithMany()
                .HasForeignKey(d => d.DelegateMemberUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(d => d.InnovationCommitteeId);
            builder.HasIndex(d => d.DelegateMemberUserId);
            builder.HasIndex(d => new { d.InnovationCommitteeId, d.StartAt, d.EndAt });
        }
    }
}
