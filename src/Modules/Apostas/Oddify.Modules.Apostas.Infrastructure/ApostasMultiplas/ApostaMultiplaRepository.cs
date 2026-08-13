using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Infrastructure.Database;

namespace Oddify.Modules.Apostas.Infrastructure.ApostasMultiplas;

internal sealed class ApostaMultiplaRepository(ApostasDbContext context) : IApostaMultiplaRepository
{
    public async Task<ApostaMultipla?> GetAsync(Guid id, Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await context.ApostasMultiplas
            .SingleOrDefaultAsync(a => a.Id == id && a.UsuarioId == usuarioId, cancellationToken);
    }

    public async Task<ApostaMultipla?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.ApostasMultiplas.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ApostaMultipla>> GetPorPassoDaJornadaAsync(Guid passoDaJornadaId, CancellationToken cancellationToken = default)
    {
        return await context.ApostasMultiplas
            .Where(a => a.PassoDaJornadaId == passoDaJornadaId)
            .ToListAsync(cancellationToken);
    }

    public void Insert(ApostaMultipla apostaMultipla)
    {
        context.ApostasMultiplas.Add(apostaMultipla);
    }

    public void Delete(ApostaMultipla apostaMultipla)
    {
        context.ApostasMultiplas.Remove(apostaMultipla);
    }
}
