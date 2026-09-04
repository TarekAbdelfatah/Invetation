using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class CommitteeMemberConfiguration : IEntityTypeConfiguration<CommitteeMember>
    {
        public void Configure(EntityTypeBuilder<CommitteeMember> builder)
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.IsHead).IsRequired();
            builder.Property(m => m.JoinedAt).IsRequired();

            builder.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.InnovationCommittee)
                .WithMany(c => c.Members)
                .HasForeignKey(m => m.InnovationCommitteeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(m => m.InnovationCommitteeId);
            builder.HasIndex(m => m.UserId);
        }
    }
}
