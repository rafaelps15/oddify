using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Oddify.Common.Infrastructure.Processing
{
    // Aplicada explicitamente em OnModelCreating de cada DbContext que usa ICommandsScheduler (via
    // AddCommandsScheduler<T>/ApplyConfiguration) — mora em Common.Infrastructure, então
    // ApplyConfigurationsFromAssembly do próprio módulo não a descobre sozinha.
    public class InternalCommandConfiguration : IEntityTypeConfiguration<InternalCommand>
    {
        public void Configure(EntityTypeBuilder<InternalCommand> builder)
        {
            builder.Property(c => c.Type).HasMaxLength(500);

            builder.Property(c => c.Content).HasColumnType("jsonb");

            // Índice parcial "covering": ordenado por EnqueuedOnUtc (bate com o ORDER BY da query de
            // polling), inclui as colunas que a query projeta (id/type/content) pra virar um Index
            // Only Scan, e filtra só comandos não processados.
            builder.HasIndex(c => new { c.EnqueuedOnUtc, c.ProcessedOnUtc })
                .HasDatabaseName("idx_internal_commands_unprocessed")
                .IncludeProperties(c => new { c.Id, c.Type, c.Content })
                .HasFilter("processed_on_utc IS NULL");
        }
    }
}
