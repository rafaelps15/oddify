using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oddify.Common.Infrastructure.Processing
{
    public class InternalCommandConfiguration : IEntityTypeConfiguration<InternalCommand>
    {
        public void Configure(EntityTypeBuilder<InternalCommand> builder)
        {
            builder.Property(c => c.Type).HasMaxLength(500);

            builder.Property(c => c.Content).HasColumnType("jsonb");

            builder.HasIndex(c => new { c.EnqueuedOnUtc, c.ProcessedOnUtc })
                .HasDatabaseName("idx_internal_commands_unprocessed")
                .IncludeProperties(c => new { c.Id, c.Type, c.Content })
                .HasFilter("processed_on_utc IS NULL");
        }
    }
}
