using MediatR;
using Oddify.Common.Application.Exceptions;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.AvaliarPassoDaJornada;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;

// Só reage quando a aposta liquidada pertence a um passo de jornada de alavancagem
// (PassoDaJornadaId setado, Origem=Alavancagem) — apostas normais (ManualEntry/ImportacaoDePrint)
// não têm PassoDaJornadaId, então saem no if abaixo sem disparar nada.
internal sealed class ApostaMultiplaLiquidadaDomainEventHandler(IApostaMultiplaRepository apostaMultiplaRepository, ISender sender)
    : IDomainEventHandler<ApostaMultiplaLiquidadaDomainEvent>
{
    public async Task Handle(ApostaMultiplaLiquidadaDomainEvent notification, CancellationToken cancellationToken)
    {
        // Re-consulta em vez de confiar só no payload do evento (que hoje só carrega
        // ApostaMultiplaId/LucroOuPerda) — mesmo critério do exemplo de domain-event handler no
        // CLAUDE.md (§10).
        ApostaMultipla? apostaMultipla =
            await apostaMultiplaRepository.GetByIdAsync(notification.ApostaMultiplaId, cancellationToken);

        if (apostaMultipla?.PassoDaJornadaId is not { } passoDaJornadaId)
        {
            return;
        }

        Result result = await sender.Send(new AvaliarPassoDaJornadaCommand(passoDaJornadaId), cancellationToken);

        if (result.IsFailure)
        {
            throw new OddifyException(nameof(AvaliarPassoDaJornadaCommand), result.Error);
        }
    }
}
