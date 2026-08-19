using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oddify.Common.Infrastructure.Outbox
{
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.Property(m => m.Type).HasMaxLength(500);

            builder.Property(m => m.Content).HasColumnType("jsonb");

            builder.HasIndex(m => new { m.OccurredOnUtc, m.ProcessedOnUtc })
                .HasDatabaseName("idx_outbox_messages_unprocessed")
                .IncludeProperties(m => new { m.Id, m.Type, m.Content })
                .HasFilter("processed_on_utc IS NULL");
        }
    }
}
