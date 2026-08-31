using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class IdeaAttachmentConfiguration : IEntityTypeConfiguration<IdeaAttachment>
    {
        public void Configure(EntityTypeBuilder<IdeaAttachment> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.FileName).IsRequired().HasMaxLength(255);
            builder.Property(a => a.ContentType).IsRequired().HasMaxLength(100);
            builder.Property(a => a.StoragePath).IsRequired().HasMaxLength(500);
            builder.Property(a => a.SizeBytes).IsRequired();
            builder.Property(a => a.UploadedAt).IsRequired();

            builder.HasOne(a => a.InnovationIdea)
                .WithMany(i => i.Attachments)
                .HasForeignKey(a => a.InnovationIdeaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.UploadedBy)
                .WithMany()
                .HasForeignKey(a => a.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
