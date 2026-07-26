using Microsoft.EntityFrameworkCore;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;
using Oddify.Modules.Apostas.Infrastructure.Database;

namespace Oddify.Modules.Apostas.Infrastructure.AnalisesDisponiveis;

internal sealed class AnaliseDisponivelParaApostaRepository(ApostasDbContext context) : IAnaliseDisponivelParaApostaRepository
{
    public async Task<AnaliseDisponivelParaAposta?> GetAsync(Guid analiseId, CancellationToken cancellationToken = default)
    {
        return await context.AnalisesDisponiveisParaAposta.SingleOrDefaultAsync(a => a.Id == analiseId, cancellationToken);
    }

    public void Insert(AnaliseDisponivelParaAposta analiseDisponivel)
    {
        context.AnalisesDisponiveisParaAposta.Add(analiseDisponivel);
    }
}
