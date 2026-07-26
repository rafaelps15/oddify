using Oddify.Modules.Fixtures.Domain.Partidas;
using Oddify.Modules.Fixtures.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Oddify.Modules.Fixtures.Infrastructure.Partidas;

internal sealed class PartidaRepository(FixturesDbContext context) : IPartidaRepository
{
    public async Task<Partida?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Partidas.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public void Insert(Partida partida)
    {
        context.Partidas.Add(partida);
    }
}
