using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Domain.Cotacoes;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.EstatisticasDeEquipe;
using Oddify.Modules.Fixtures.Domain.EstatisticasDeJogador;
using Oddify.Modules.Fixtures.Domain.Jogadores;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Infrastructure.Database;

public sealed class FixturesDbContext(DbContextOptions<FixturesDbContext> options) : DbContext(options), IUnitOfWork
{
    internal DbSet<LigaConfigurada> Ligas { get; set; }

    internal DbSet<Equipe> Equipes { get; set; }

    internal DbSet<Jogador> Jogadores { get; set; }

    internal DbSet<Partida> Partidas { get; set; }

    internal DbSet<EstatisticaEquipe> EstatisticasDeEquipe { get; set; }

    internal DbSet<EstatisticaJogador> EstatisticasDeJogador { get; set; }

    internal DbSet<Cotacao> Cotacoes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Fixtures);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FixturesDbContext).Assembly);
    }
}
