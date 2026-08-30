using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class UserTypeLookupConfiguration : IEntityTypeConfiguration<UserTypeLookup>
    {
        public void Configure(EntityTypeBuilder<UserTypeLookup> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Code).IsRequired().HasMaxLength(50);
            builder.HasIndex(u => u.Code).IsUnique();
            builder.Property(u => u.Name).IsRequired().HasMaxLength(100);
            builder.Property(u => u.IsActive).IsRequired();
            builder.Property(u => u.DisplayOrder).IsRequired();
            builder.Property(u => u.CreatedAt).IsRequired();
        }
    }
}