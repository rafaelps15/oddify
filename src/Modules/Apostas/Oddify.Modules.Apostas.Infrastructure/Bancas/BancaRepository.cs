using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Infrastructure.Database;

namespace Oddify.Modules.Apostas.Infrastructure.Bancas;

internal sealed class BancaRepository(ApostasDbContext context) : IBancaRepository
{
    public async Task<Banca?> GetAsync(Guid id, Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await context.Bancas.SingleOrDefaultAsync(b => b.Id == id && b.UsuarioId == usuarioId, cancellationToken);
    }

    public async Task<bool> ExistsForUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await context.Bancas.AnyAsync(b => b.UsuarioId == usuarioId, cancellationToken);
    }

    public void Insert(Banca banca)
    {
        context.Bancas.Add(banca);
    }
}
