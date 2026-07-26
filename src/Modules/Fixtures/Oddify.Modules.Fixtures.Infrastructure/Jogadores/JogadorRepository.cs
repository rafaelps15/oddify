using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Fixtures.Domain.Jogadores;
using Oddify.Modules.Fixtures.Infrastructure.Database;

namespace Oddify.Modules.Fixtures.Infrastructure.Jogadores;

internal sealed class JogadorRepository(FixturesDbContext context) : IJogadorRepository
{
    public async Task<Jogador?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Jogadores.SingleOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public void Insert(Jogador jogador)
    {
        context.Jogadores.Add(jogador);
    }
}
