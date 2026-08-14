using MassTransit;
using MediatR;
using Oddify.Common.Application.Exceptions;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Bancas.CriarBancaInicial;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.Modules.Apostas.Presentation.IntegrationEvents;

public sealed class EmailVerifiedIntegrationEventConsumer(ISender sender) : IConsumer<EmailVerifiedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<EmailVerifiedIntegrationEvent> context)
    {
        EmailVerifiedIntegrationEvent evento = context.Message;

        Result result = await sender.Send(
            new CriarBancaInicialCommand(evento.UserId, evento.OccurredOnUtc),
            context.CancellationToken);

        if (result.IsFailure)
        {
            throw new OddifyException(nameof(CriarBancaInicialCommand), result.Error);
        }
    }
}
