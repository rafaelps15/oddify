using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;
using Oddify.Modules.Apostas.Domain.PassosDaJornada;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.Modules.Apostas.Infrastructure.Database;

public sealed class ApostasDbContext(DbContextOptions<ApostasDbContext> options) : DbContext(options), IUnitOfWork
{
    internal DbSet<Banca> Bancas { get; set; }

    internal DbSet<ApostaMultipla> ApostasMultiplas { get; set; }

    internal DbSet<PernaDeAposta> PernasDeAposta { get; set; }

    internal DbSet<AnaliseDisponivelParaAposta> AnalisesDisponiveisParaAposta { get; set; }

    internal DbSet<MovimentacaoDaBanca> MovimentacoesDaBanca { get; set; }

    internal DbSet<JornadaDeAlavancagem> JornadasDeAlavancagem { get; set; }

    internal DbSet<PassoDaJornada> PassosDaJornada { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Apostas);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApostasDbContext).Assembly);
    }
}
