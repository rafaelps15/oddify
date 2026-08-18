using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oddify.Common.Infrastructure.Inbox;

// Aplicada explicitamente em OnModelCreating de qualquer DbContext que consome integration event de
// outro módulo (via AddInbox<TContext>/ApplyConfiguration) — mora em Common.Infrastructure, então
// ApplyConfigurationsFromAssembly do próprio módulo não a descobre sozinha.
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.Property(m => m.Type).HasMaxLength(500);

        builder.Property(m => m.Content).HasColumnType("jsonb");

        builder.HasIndex(m => new { m.OccurredOnUtc, m.ProcessedOnUtc })
            .HasDatabaseName("idx_inbox_messages_unprocessed")
            .IncludeProperties(m => new { m.Id, m.Type, m.Content })
            .HasFilter("processed_on_utc IS NULL");
    }
}
