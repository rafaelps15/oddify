using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Calculo;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.Modules.Apostas.Application.Bancas.GetSugestaoDeStake;

// Preview read-only da mesma conta que MontarMultiplaCommandHandler faz de verdade ao montar uma
// aposta (mesmo KellyCalculator, mesma FracaoDeKelly/TetoDeStakeSobreABanca fixos) — existe pra
// a tela "Calculadora de Alavancagem" do front parar de reimplementar essa fórmula do zero (o
// front tinha uma fração de Kelly ajustável pelo usuário e nenhum teto, então sugeria valores que
// a montagem real de aposta nunca respeitaria). Puramente aritmético — sem IDbConnectionFactory,
// não busca nada no banco (ver comentário em GetSugestaoDeStakeQuery sobre SaldoDaBanca).
internal sealed class GetSugestaoDeStakeQueryHandler : IQueryHandler<GetSugestaoDeStakeQuery, SugestaoDeStakeResponse>
{
    public Task<Result<SugestaoDeStakeResponse>> Handle(GetSugestaoDeStakeQuery request, CancellationToken cancellationToken)
    {
        if (request.Odd <= 1m)
        {
            return Task.FromResult(Result.Failure<SugestaoDeStakeResponse>(BancaErrors.OddInvalida));
        }

        decimal fracaoDeKelly = request.FracaoDeKelly ?? KellyCalculator.FracaoDeKelly;
        decimal probabilidadeImplicita = 1m / request.Odd;
        decimal vantagem = request.Probabilidade - probabilidadeImplicita;
        decimal stakeSugerido = KellyCalculator.CalcularStake(request.SaldoDaBanca, request.Probabilidade, request.Odd, fracaoDeKelly);

        var response = new SugestaoDeStakeResponse(
            probabilidadeImplicita,
            vantagem,
            stakeSugerido,
            fracaoDeKelly,
            KellyCalculator.TetoDeStakeSobreABanca);

        return Task.FromResult<Result<SugestaoDeStakeResponse>>(response);
    }
}
