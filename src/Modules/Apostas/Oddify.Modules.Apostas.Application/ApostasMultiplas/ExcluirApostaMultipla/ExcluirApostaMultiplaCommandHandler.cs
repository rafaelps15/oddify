using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.ExcluirApostaMultipla;

// Exclusão de verdade (a linha some), diferente de EstornarLiquidacaoMultipla (a aposta volta pra
// Pendente e continua existindo). Quando a aposta já estava liquidada, reaproveita
// ApostaMultipla.Estornar pra reverter o saldo antes de apagar a linha — o registro contábil da
// reversão (MovimentacaoDaBanca tipo Estorno) fica, mesmo depois que a aposta em si é excluída,
// porque ApostaMultiplaId em MovimentacaoDaBanca não tem FK física (ver
// MovimentacaoDaBancaConfiguration) e o extrato precisa continuar íntegro.
internal sealed class ExcluirApostaMultiplaCommandHandler(
    IApostaMultiplaRepository apostaMultiplaRepository,
    IPernaDeApostaRepository pernaDeApostaRepository,
    IAnaliseDisponivelParaApostaRepository analiseDisponivelParaApostaRepository,
    IBancaRepository bancaRepository,
    IMovimentacaoDaBancaRepository movimentacaoDaBancaRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<ExcluirApostaMultiplaCommand>
{
    public async Task<Result> Handle(ExcluirApostaMultiplaCommand request, CancellationToken cancellationToken)
    {
        ApostaMultipla? apostaMultipla = await apostaMultiplaRepository.GetAsync(request.ApostaMultiplaId, userContext.UserId, cancellationToken);
        if (apostaMultipla is null)
        {
            return Result.Failure(ApostaMultiplaErrors.NotFound(request.ApostaMultiplaId));
        }

        DateTime agora = dateTimeProvider.UtcNow;

        if (apostaMultipla.Resultado != ResultadoDaAposta.Pendente)
        {
            Result<decimal> estornarResult = apostaMultipla.Estornar(agora);
            if (estornarResult.IsFailure)
            {
                return Result.Failure(estornarResult.Error);
            }

            Banca? banca = await bancaRepository.GetAsync(apostaMultipla.BancaId, userContext.UserId, cancellationToken);
            if (banca is null)
            {
                return Result.Failure(BancaErrors.NotFound(apostaMultipla.BancaId));
            }

            decimal reversao = -estornarResult.Value;

            MovimentacaoDaBanca movimentacao = banca.RegistrarMovimentacao(reversao, TipoDeMovimentacao.Estorno, apostaMultipla.Id, agora);
            movimentacaoDaBancaRepository.Insert(movimentacao);
        }

        IReadOnlyCollection<PernaDeAposta> pernas =
            await pernaDeApostaRepository.GetPorApostaMultiplaAsync(request.ApostaMultiplaId, cancellationToken);

        foreach (PernaDeAposta perna in pernas)
        {
            AnaliseDisponivelParaAposta? analiseDisponivel =
                await analiseDisponivelParaApostaRepository.GetAsync(perna.AnaliseId, cancellationToken);
            if (analiseDisponivel is null)
            {
                return Result.Failure(AnaliseDisponivelParaApostaErrors.NotFound(perna.AnaliseId));
            }

            Result liberarResult = analiseDisponivel.LiberarUso();
            if (liberarResult.IsFailure)
            {
                return liberarResult;
            }
        }

        apostaMultiplaRepository.Delete(apostaMultipla);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
