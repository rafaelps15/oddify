using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Fixtures.Domain.Escalacoes;
using Oddify.Modules.Fixtures.Infrastructure.Database;

namespace Oddify.Modules.Fixtures.Infrastructure.Escalacoes;

internal sealed class EscalacaoRepository(FixturesDbContext context) : IEscalacaoRepository
{
    public async Task<Escalacao?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Escalacoes.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public void Insert(Escalacao escalacao)
    {
        context.Escalacoes.Add(escalacao);
    }
}
