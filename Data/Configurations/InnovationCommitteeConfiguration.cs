using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class InnovationCommitteeConfiguration : IEntityTypeConfiguration<InnovationCommittee>
    {
        public void Configure(EntityTypeBuilder<InnovationCommittee> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Description).HasMaxLength(2000);
            builder.Property(c => c.CreatedAt).IsRequired();

            builder.HasOne(c => c.CreatedBy)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Members)
                .WithOne(m => m.InnovationCommittee)
                .HasForeignKey(m => m.InnovationCommitteeId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(c => c.IsActive);
        }
    }
}
