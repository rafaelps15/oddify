using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Oddify.Modules.Apostas.Infrastructure.Bancas;

internal sealed class BancaRepository(ApostasDbContext context) : IBancaRepository
{
    public async Task<Banca?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Bancas.SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public void Insert(Banca banca)
    {
        context.Bancas.Add(banca);
    }
}
