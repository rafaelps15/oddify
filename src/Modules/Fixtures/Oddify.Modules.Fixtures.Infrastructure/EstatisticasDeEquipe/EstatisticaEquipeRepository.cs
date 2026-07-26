using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Fixtures.Domain.EstatisticasDeEquipe;
using Oddify.Modules.Fixtures.Infrastructure.Database;

namespace Oddify.Modules.Fixtures.Infrastructure.EstatisticasDeEquipe;

internal sealed class EstatisticaEquipeRepository(FixturesDbContext context) : IEstatisticaEquipeRepository
{
    public async Task<EstatisticaEquipe?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.EstatisticasDeEquipe.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public void Insert(EstatisticaEquipe estatistica)
    {
        context.EstatisticasDeEquipe.Add(estatistica);
    }
}
