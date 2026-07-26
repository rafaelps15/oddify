using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Fixtures.Domain.Cotacoes;
using Oddify.Modules.Fixtures.Infrastructure.Database;

namespace Oddify.Modules.Fixtures.Infrastructure.Cotacoes;

internal sealed class CotacaoRepository(FixturesDbContext context) : ICotacaoRepository
{
    public async Task<Cotacao?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Cotacoes.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public void Insert(Cotacao cotacao)
    {
        context.Cotacoes.Add(cotacao);
    }
}
