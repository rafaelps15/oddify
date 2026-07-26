using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Fixtures.Domain.Partidas;
using Oddify.Modules.Fixtures.Infrastructure.Database;

namespace Oddify.Modules.Fixtures.Infrastructure.Partidas;

internal sealed class PartidaRepository(FixturesDbContext context) : IPartidaRepository
{
    public async Task<Partida?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Partidas.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Partida?> GetByIdExternoAsync(string idExterno, CancellationToken cancellationToken = default)
    {
        return await context.Partidas.SingleOrDefaultAsync(p => p.IdExterno == idExterno, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Partida>> ListarAgendadasEntreAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken = default)
    {
        return await context.Partidas
            .Where(p => p.Situacao == SituacaoDaPartida.Agendada && p.DataUtc >= inicioUtc && p.DataUtc <= fimUtc)
            .ToListAsync(cancellationToken);
    }

    public void Insert(Partida partida)
    {
        context.Partidas.Add(partida);
    }
}
