using Oddify.Modules.Fixtures.Domain.EstatisticasDeJogador;
using Oddify.Modules.Fixtures.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Oddify.Modules.Fixtures.Infrastructure.EstatisticasDeJogador;

internal sealed class EstatisticaJogadorRepository(FixturesDbContext context) : IEstatisticaJogadorRepository
{
    public async Task<EstatisticaJogador?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.EstatisticasDeJogador.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public void Insert(EstatisticaJogador estatistica)
    {
        context.EstatisticasDeJogador.Add(estatistica);
    }
}
