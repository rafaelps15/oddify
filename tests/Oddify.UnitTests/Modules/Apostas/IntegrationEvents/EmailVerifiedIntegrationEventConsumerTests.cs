using FluentAssertions;
using MassTransit;
using MediatR;
using NSubstitute;
using Oddify.Common.Application.Exceptions;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Bancas.CriarBancaInicial;
using Oddify.Modules.Apostas.Presentation.IntegrationEvents;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.UnitTests.Modules.Apostas.IntegrationEvents;

public sealed class EmailVerifiedIntegrationEventConsumerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    private EmailVerifiedIntegrationEventConsumer CriarConsumer() => new(_sender);

    private static ConsumeContext<EmailVerifiedIntegrationEvent> CriarContexto(EmailVerifiedIntegrationEvent evento)
    {
        ConsumeContext<EmailVerifiedIntegrationEvent> context = Substitute.For<ConsumeContext<EmailVerifiedIntegrationEvent>>();
        context.Message.Returns(evento);
        context.CancellationToken.Returns(CancellationToken.None);
        return context;
    }

    [Fact]
    public async Task Consume_should_dispatch_criar_banca_inicial_command()
    {
        var evento = new EmailVerifiedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid());
        _sender.Send(Arg.Any<CriarBancaInicialCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        await CriarConsumer().Consume(CriarContexto(evento));

        await _sender.Received(1).Send(
            Arg.Is<CriarBancaInicialCommand>(c => c.UsuarioId == evento.UserId && c.OcorridoEmUtc == evento.OccurredOnUtc),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_should_throw_when_command_fails()
    {
        var evento = new EmailVerifiedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid());
        _sender.Send(Arg.Any<CriarBancaInicialCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Failure("Bancas.Falha", "falha simulada")));

        Func<Task> act = () => CriarConsumer().Consume(CriarContexto(evento));

        await act.Should().ThrowAsync<OddifyException>();
    }
}
