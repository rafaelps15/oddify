using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Analise.Infrastructure.Database;

namespace Oddify.Modules.Analise.Infrastructure.Analises;

internal sealed class AnaliseDePartidaRepository(AnaliseDbContext context) : IAnaliseDePartidaRepository
{
    public async Task<AnaliseDePartida?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.AnalisesDePartida.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public void Insert(AnaliseDePartida analise)
    {
        context.AnalisesDePartida.Add(analise);
    }
}
