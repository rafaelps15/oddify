using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Analise.Domain.Fixtures;
using Oddify.Modules.Analise.Infrastructure.Database;

namespace Oddify.Modules.Analise.Infrastructure.Fixtures;

internal sealed class LigaRepository(AnaliseDbContext context) : ILigaRepository
{
    public async Task<Liga?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Ligas.SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public void Insert(Liga liga)
    {
        context.Ligas.Add(liga);
    }
}
