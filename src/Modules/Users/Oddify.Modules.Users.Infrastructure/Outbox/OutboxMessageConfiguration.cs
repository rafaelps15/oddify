using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oddify.Modules.Users.Infrastructure.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.Property(m => m.Type).HasMaxLength(500);

        builder.Property(m => m.Content).HasColumnType("jsonb");

        // Índice parcial "covering": ordenado por OccurredOnUtc (bate com o ORDER BY da query
        // de polling), inclui as colunas que a query projeta (id/type/content) pra virar um
        // Index Only Scan, e filtra só mensagens não processadas.
        builder.HasIndex(m => new { m.OccurredOnUtc, m.ProcessedOnUtc })
            .HasDatabaseName("idx_outbox_messages_unprocessed")
            .IncludeProperties(m => new { m.Id, m.Type, m.Content })
            .HasFilter("processed_on_utc IS NULL");
    }
}
