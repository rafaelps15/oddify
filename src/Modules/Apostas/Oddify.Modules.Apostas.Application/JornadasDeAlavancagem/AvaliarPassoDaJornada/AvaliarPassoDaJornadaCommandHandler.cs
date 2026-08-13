using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.Calculo;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;
using Oddify.Modules.Apostas.Domain.PassosDaJornada;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.AvaliarPassoDaJornada;

// Disparado automaticamente por ApostaMultiplaLiquidadaDomainEventHandler assim que a última das N
// apostas de um passo liquida (ver Application/ApostasMultiplas/LiquidarMultipla/) — mas também
// pode ser chamado à mão (endpoint) pra reavaliar um passo preso, então a idempotência (passo
// precisa estar EmAberto) é conferida aqui, não só confiada em quem dispara.
internal sealed class AvaliarPassoDaJornadaCommandHandler(
    IPassoDaJornadaRepository passoDaJornadaRepository,
    IJornadaDeAlavancagemRepository jornadaDeAlavancagemRepository,
    IBancaRepository bancaRepository,
    IApostaMultiplaRepository apostaMultiplaRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AvaliarPassoDaJornadaCommand>
{
    // Regra da faixa: pelo menos 2 das N apostas do passo precisam ganhar pra avançar — ver
    // RegrasDeAlavancagem.CalcularProbabilidadeDeAvancoPorPasso, que deriva os %s da spec a partir
    // desse mesmo limiar.
    private const int VitoriasMinimasParaAvancar = 2;

    public async Task<Result> Handle(AvaliarPassoDaJornadaCommand request, CancellationToken cancellationToken)
    {
        PassoDaJornada? passo = await passoDaJornadaRepository.GetAsync(request.PassoId, cancellationToken);
        if (passo is null)
        {
            return Result.Failure(PassoDaJornadaErrors.NotFound(request.PassoId));
        }

        if (passo.Status != StatusDoPasso.EmAberto)
        {
            // Já avaliado (reenvio do evento, ou chamada manual duplicada) — não é erro, só não há
            // nada a fazer de novo.
            return Result.Success();
        }

        IReadOnlyCollection<ApostaMultipla> apostas =
            await apostaMultiplaRepository.GetPorPassoDaJornadaAsync(passo.Id, cancellationToken);

        if (apostas.Any(a => a.Resultado == ResultadoDaAposta.Pendente))
        {
            // Ainda faltam apostas do passo liquidarem — reavalia quando a próxima liquidar.
            return Result.Success();
        }

        JornadaDeAlavancagem? jornada = await jornadaDeAlavancagemRepository.GetByIdAsync(passo.JornadaId, cancellationToken);
        if (jornada is null)
        {
            return Result.Failure(JornadaDeAlavancagemErrors.NotFound(passo.JornadaId));
        }

        Banca? banca = await bancaRepository.GetAsync(jornada.BancaId, jornada.UsuarioId, cancellationToken);
        if (banca is null)
        {
            return Result.Failure(BancaErrors.NotFound(jornada.BancaId));
        }

        int vitorias = apostas.Count(a => a.Resultado == ResultadoDaAposta.Ganha);

        // A banca inteira está sempre alocada no passo corrente — depois que as N apostas
        // liquidam (cada uma já ajustou o saldo via LiquidarMultiplaCommandHandler), SaldoAtual
        // já É o valor resultante do passo, sem precisar recompor a partir das apostas.
        decimal valorResultante = banca.SaldoAtual;

        DateTime agora = dateTimeProvider.UtcNow;

        if (vitorias >= VitoriasMinimasParaAvancar)
        {
            Result marcarAvancouResult = passo.MarcarAvancou(valorResultante);
            if (marcarAvancouResult.IsFailure)
            {
                return marcarAvancouResult;
            }

            Result progressoResult = jornada.PassoAtual >= jornada.TotalDePassos
                ? jornada.Concluir(agora)
                : jornada.AvancarPasso(agora);

            if (progressoResult.IsFailure)
            {
                return progressoResult;
            }
        }
        else
        {
            Result marcarQuebrouResult = passo.MarcarQuebrou(valorResultante);
            if (marcarQuebrouResult.IsFailure)
            {
                return marcarQuebrouResult;
            }

            decimal bancaMinima = RegrasDeAlavancagem.CalcularBancaMinima(jornada.NumeroDeFracoes);

            // 0 vitórias, ou o valor recomposto não cobre mais nem a banca mínima da faixa: a
            // jornada encerra aqui. Com 1 vitória e valor ainda >= mínimo, a jornada continua
            // EmAndamento (PassoAtual não muda) — a próxima chamada a MontarPassoDaJornada monta
            // uma nova tentativa do mesmo passo com o saldo reduzido que sobrou.
            if (valorResultante < bancaMinima)
            {
                Result marcarQuebradaResult = jornada.MarcarQuebrada(agora);
                if (marcarQuebradaResult.IsFailure)
                {
                    return marcarQuebradaResult;
                }
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
