using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Analise.Domain.Fixtures;
using Oddify.Modules.Analise.Infrastructure.Database;

namespace Oddify.Modules.Analise.Infrastructure.Fixtures;

internal sealed class CotacaoRepository(AnaliseDbContext context) : ICotacaoRepository
{
    public async Task<Cotacao?> GetMaisRecenteAsync(Guid partidaId, string mercado, CancellationToken cancellationToken = default)
    {
        return await context.Cotacoes
            .Where(c => c.PartidaId == partidaId && c.Mercado == mercado)
            .OrderByDescending(c => c.ColetadaEmUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Cotacao>> GetPorPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default)
    {
        return await context.Cotacoes
            .Where(c => c.PartidaId == partidaId)
            .OrderByDescending(c => c.ColetadaEmUtc)
            .ToListAsync(cancellationToken);
    }

    public void Insert(Cotacao cotacao)
    {
        context.Cotacoes.Add(cotacao);
    }
}
