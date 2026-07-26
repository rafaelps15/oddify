using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Infrastructure.Database;

namespace Oddify.Modules.Apostas.Infrastructure.ApostasMultiplas;

internal sealed class ApostaMultiplaRepository(ApostasDbContext context) : IApostaMultiplaRepository
{
    public async Task<ApostaMultipla?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.ApostasMultiplas.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public void Insert(ApostaMultipla apostaMultipla)
    {
        context.ApostasMultiplas.Add(apostaMultipla);
    }
}
