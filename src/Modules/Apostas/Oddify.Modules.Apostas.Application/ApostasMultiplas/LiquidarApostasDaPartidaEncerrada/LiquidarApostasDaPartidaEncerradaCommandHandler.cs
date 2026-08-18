using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarApostasDaPartidaEncerrada;

// Disparado pelo PartidaEncerradaIntegrationEventConsumer (Presentation/IntegrationEvents) — melhor
// esforço, não tudo ou nada: uma múltipla com outra perna ainda pendente em partida diferente falha
// em ApostaMultiplaLiquidacaoService.LiquidarAsync (partida não encerrada) e é simplesmente
// ignorada aqui, ficando pra ser resolvida quando o PartidaEncerradaIntegrationEvent daquela outra
// partida chegar. Não há Result.Failure que valha a pena propagar pro consumer: nenhuma falha
// individual aqui é um bug a ser investigado, é o estado normal de uma múltipla ainda incompleta.
internal sealed class LiquidarApostasDaPartidaEncerradaCommandHandler(
    IApostaMultiplaRepository apostaMultiplaRepository,
    ApostaMultiplaLiquidacaoService liquidacaoService,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LiquidarApostasDaPartidaEncerradaCommand>
{
    public async Task<Result> Handle(LiquidarApostasDaPartidaEncerradaCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ApostaMultipla> apostasPendentes =
            await apostaMultiplaRepository.GetPendentesPorPartidaAsync(request.PartidaId, cancellationToken);

        foreach (ApostaMultipla apostaMultipla in apostasPendentes)
        {
            Result liquidarResult = await liquidacaoService.LiquidarAsync(apostaMultipla, cancellationToken);
            if (liquidarResult.IsFailure)
            {
                continue;
            }

            // Salva por item, não uma vez no fim do laço: uma múltipla liquidada com sucesso não
            // pode ficar presa na mesma transação de uma múltipla seguinte que falhar.
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
