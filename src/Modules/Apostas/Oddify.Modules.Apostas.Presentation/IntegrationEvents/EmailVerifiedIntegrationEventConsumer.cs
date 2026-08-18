using MediatR;
using Oddify.Common.Application.EventBus;
using Oddify.Common.Application.Exceptions;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Bancas.CriarBancaInicial;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.Modules.Apostas.Presentation.IntegrationEvents;

// Despachado pelo ProcessInboxJob — ver comentário equivalente em
// AnaliseConfirmadaIntegrationEventConsumer.
public sealed class EmailVerifiedIntegrationEventConsumer(ISender sender) : IntegrationEventHandler<EmailVerifiedIntegrationEvent>
{
    public override async Task Handle(EmailVerifiedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        Result result = await sender.Send(
            new CriarBancaInicialCommand(integrationEvent.UserId, integrationEvent.OccurredOnUtc),
            cancellationToken);

        if (result.IsFailure)
        {
            throw new OddifyException(nameof(CriarBancaInicialCommand), result.Error);
        }
    }
}
