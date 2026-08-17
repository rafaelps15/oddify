using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;
using Oddify.Modules.Apostas.Infrastructure.Database;

namespace Oddify.Modules.Apostas.Infrastructure.JornadasDeAlavancagem;

internal sealed class FaixaDeMetaCatalogoRepository(ApostasDbContext context) : IFaixaDeMetaCatalogoRepository
{
    public async Task<FaixaDeMetaCatalogo?> GetAsync(FaixaDeMeta faixa, CancellationToken cancellationToken = default)
    {
        return await context.FaixasDeMetaCatalogo.SingleOrDefaultAsync(f => f.Faixa == faixa, cancellationToken);
    }
}
