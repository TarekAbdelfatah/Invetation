using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ibtikar.Data.Configurations
{
    public class ExecutionStageConfiguration : IEntityTypeConfiguration<ExecutionStage>
    {
        public void Configure(EntityTypeBuilder<ExecutionStage> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Order).IsRequired();
            builder.HasIndex(s => s.Order).IsUnique();
            builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
            builder.HasIndex(s => s.Code).IsUnique();
            builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
            builder.Property(s => s.IsActive).IsRequired();
            builder.Property(s => s.CreatedAt).IsRequired();
        }
    }
}