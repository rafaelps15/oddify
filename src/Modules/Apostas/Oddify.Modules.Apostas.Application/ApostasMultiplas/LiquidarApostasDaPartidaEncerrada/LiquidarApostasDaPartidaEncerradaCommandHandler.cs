using MediatR;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarApostasDaPartidaEncerrada;

// Disparado pelo PartidaEncerradaIntegrationEventConsumer (Presentation/IntegrationEvents) — melhor
// esforço, não tudo ou nada: reenvia o mesmo LiquidarMultiplaCommand que o endpoint usa, um por aposta
// pendente (mesmo padrão do modular-monolith-with-ddd para reagir a um evento em lote — reenviar o
// Command, não duplicar ou compartilhar a lógica de liquidação numa classe à parte). Uma múltipla com
// outra perna ainda pendente em partida diferente falha dentro do LiquidarMultiplaCommandHandler
// (partida não encerrada) e é simplesmente ignorada aqui, ficando pra ser resolvida quando o
// PartidaEncerradaIntegrationEvent daquela outra partida chegar. Não há Result.Failure que valha a pena
// propagar pro consumer: nenhuma falha individual aqui é um bug a ser investigado, é o estado normal de
// uma múltipla ainda incompleta.
internal sealed class LiquidarApostasDaPartidaEncerradaCommandHandler(
    IApostaMultiplaRepository apostaMultiplaRepository,
    ISender sender)
    : ICommandHandler<LiquidarApostasDaPartidaEncerradaCommand>
{
    public async Task<Result> Handle(LiquidarApostasDaPartidaEncerradaCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ApostaMultipla> apostasPendentes =
            await apostaMultiplaRepository.GetPendentesPorPartidaAsync(request.PartidaId, cancellationToken);

        foreach (ApostaMultipla apostaMultipla in apostasPendentes)
        {
            await sender.Send(new LiquidarMultiplaCommand(apostaMultipla.Id, apostaMultipla.UsuarioId), cancellationToken);
        }

        return Result.Success();
    }
}
