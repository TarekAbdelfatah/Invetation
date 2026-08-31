using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class CommitteeVoteConfiguration : IEntityTypeConfiguration<CommitteeVote>
    {
        public void Configure(EntityTypeBuilder<CommitteeVote> builder)
        {
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Decision).IsRequired().HasMaxLength(30);
            builder.Property(v => v.Note).HasMaxLength(2000);
            builder.Property(v => v.VotedAt).IsRequired();

            builder.HasOne(v => v.InnovationIdea)
                .WithMany()
                .HasForeignKey(v => v.InnovationIdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(v => v.Member)
                .WithMany()
                .HasForeignKey(v => v.MemberUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(v => v.InnovationIdeaId);
            builder.HasIndex(v => v.MemberUserId);
            builder.HasIndex(v => new { v.InnovationIdeaId, v.MemberUserId }).IsUnique();
        }
    }
}
