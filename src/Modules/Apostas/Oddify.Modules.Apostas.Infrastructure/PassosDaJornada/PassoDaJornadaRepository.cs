using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Apostas.Domain.PassosDaJornada;
using Oddify.Modules.Apostas.Infrastructure.Database;

namespace Oddify.Modules.Apostas.Infrastructure.PassosDaJornada;

internal sealed class PassoDaJornadaRepository(ApostasDbContext context) : IPassoDaJornadaRepository
{
    public async Task<PassoDaJornada?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.PassosDaJornada.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<PassoDaJornada?> GetEmAbertoPorJornadaAsync(Guid jornadaId, CancellationToken cancellationToken = default)
    {
        return await context.PassosDaJornada
            .Where(p => p.JornadaId == jornadaId && p.Status == StatusDoPasso.EmAberto)
            .OrderByDescending(p => p.CriadoEmUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Insert(PassoDaJornada passoDaJornada)
    {
        context.PassosDaJornada.Add(passoDaJornada);
    }
}
