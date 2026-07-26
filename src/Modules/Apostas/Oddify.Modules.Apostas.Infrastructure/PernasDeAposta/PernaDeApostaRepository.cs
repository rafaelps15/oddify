using Oddify.Modules.Apostas.Domain.PernasDeAposta;
using Oddify.Modules.Apostas.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Oddify.Modules.Apostas.Infrastructure.PernasDeAposta;

internal sealed class PernaDeApostaRepository(ApostasDbContext context) : IPernaDeApostaRepository
{
    public async Task<PernaDeAposta?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.PernasDeAposta.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public void Insert(PernaDeAposta pernaDeAposta)
    {
        context.PernasDeAposta.Add(pernaDeAposta);
    }

    public async Task<IReadOnlyCollection<PernaDeAposta>> GetPorApostaMultiplaAsync(
        Guid apostaMultiplaId,
        CancellationToken cancellationToken = default)
    {
        return await context.PernasDeAposta
            .Where(p => p.ApostaMultiplaId == apostaMultiplaId)
            .ToListAsync(cancellationToken);
    }
}
