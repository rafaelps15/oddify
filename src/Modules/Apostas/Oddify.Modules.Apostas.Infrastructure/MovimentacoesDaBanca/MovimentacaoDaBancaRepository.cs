using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;
using Oddify.Modules.Apostas.Infrastructure.Database;

namespace Oddify.Modules.Apostas.Infrastructure.MovimentacoesDaBanca;

internal sealed class MovimentacaoDaBancaRepository(ApostasDbContext context) : IMovimentacaoDaBancaRepository
{
    public void Insert(MovimentacaoDaBanca movimentacao)
    {
        context.MovimentacoesDaBanca.Add(movimentacao);
    }
}
