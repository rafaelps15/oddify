using Microsoft.EntityFrameworkCore;
using Oddify.Common.Infrastructure.Outbox;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.Modules.Analise.Infrastructure.Database;

public sealed class AnaliseDbContext(DbContextOptions<AnaliseDbContext> options) : DbContext(options), IUnitOfWork
{
    internal DbSet<AnaliseDePartida> AnalisesDePartida { get; set; }

    internal DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Analise);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AnaliseDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
