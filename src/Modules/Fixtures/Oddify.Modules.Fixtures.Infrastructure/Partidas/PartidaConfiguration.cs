using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Infrastructure.Partidas;

internal sealed class PartidaConfiguration : IEntityTypeConfiguration<Partida>
{
    public void Configure(EntityTypeBuilder<Partida> builder)
    {
        builder.Property(p => p.IdExterno).HasMaxLength(100);

        // O "upsert" de SincronizarFixturesDaLigaCommandHandler é check-then-insert em nível de
        // aplicação (GetByIdExternoAsync seguido de Insert), sem lock — duas execuções concorrentes
        // (job periódico + chamada manual, ou dois ciclos sobrepostos) podiam duplicar a partida.
        // Este índice único garante a idempotência de verdade no banco.
        builder.HasIndex(p => p.IdExterno).IsUnique();

        // Suporta os filtros de liga+temporada+rodada da tela de Partidas (GetPartidasQuery,
        // GetRodadasDisponiveisQuery, GetRodadaMaisRecenteEncerradaQuery) — não é unique, várias
        // partidas compartilham a mesma combinação.
        builder.HasIndex(p => new { p.LigaId, p.Temporada, p.Rodada });

        builder.HasOne<LigaConfigurada>().WithMany().HasForeignKey(p => p.LigaId);

        builder.HasOne<Equipe>()
            .WithMany()
            .HasForeignKey(p => p.EquipeCasaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Equipe>()
            .WithMany()
            .HasForeignKey(p => p.EquipeVisitanteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
