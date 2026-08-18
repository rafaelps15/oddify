using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Fixtures.Domain.EscalacoesDeJogador;
using Oddify.Modules.Fixtures.Infrastructure.Database;

namespace Oddify.Modules.Fixtures.Infrastructure.EscalacoesDeJogador;

internal sealed class EscalacaoJogadorRepository(FixturesDbContext context) : IEscalacaoJogadorRepository
{
    public async Task<EscalacaoJogador?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.EscalacoesDeJogador.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public void Insert(EscalacaoJogador escalacaoJogador)
    {
        context.EscalacoesDeJogador.Add(escalacaoJogador);
    }
}
