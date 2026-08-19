using Microsoft.EntityFrameworkCore;
using Oddify.Common.Infrastructure.Inbox;
using Oddify.Common.Infrastructure.Outbox;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.Modules.Analise.Infrastructure.Database;

public sealed class AnaliseDbContext(DbContextOptions<AnaliseDbContext> options) : DbContext(options), IUnitOfWork
{
    internal DbSet<AnaliseDePartida> AnalisesDePartida { get; set; }

    internal DbSet<Liga> Ligas { get; set; }

    internal DbSet<Partida> Partidas { get; set; }

    internal DbSet<Cotacao> Cotacoes { get; set; }

    internal DbSet<OutboxMessage> OutboxMessages { get; set; }

    internal DbSet<InboxMessage> InboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Analise);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnaliseDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
    }
}
