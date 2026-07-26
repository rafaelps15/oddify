using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Oddify.Modules.Fixtures.Infrastructure.Ligas;

internal sealed class LigaConfiguradaRepository(FixturesDbContext context) : ILigaConfiguradaRepository
{
    public async Task<LigaConfigurada?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Ligas.SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<LigaConfigurada?> GetByIdExternoAsync(string idExterno, CancellationToken cancellationToken = default)
    {
        return await context.Ligas.SingleOrDefaultAsync(l => l.IdExterno == idExterno, cancellationToken);
    }

    public void Insert(LigaConfigurada liga)
    {
        context.Ligas.Add(liga);
    }
}
