using FluentAssertions;
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

    [Fact]
    public async Task Handle_should_dispatch_criar_banca_inicial_command()
    {
        var evento = new EmailVerifiedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid());
        _sender.Send(Arg.Any<CriarBancaInicialCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        await CriarConsumer().Handle(evento, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<CriarBancaInicialCommand>(c => c.UsuarioId == evento.UserId && c.OcorridoEmUtc == evento.OccurredOnUtc),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_throw_when_command_fails()
    {
        var evento = new EmailVerifiedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid());
        _sender.Send(Arg.Any<CriarBancaInicialCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Failure("Bancas.Falha", "falha simulada")));

        Func<Task> act = () => CriarConsumer().Handle(evento, CancellationToken.None);

        await act.Should().ThrowAsync<OddifyException>();
    }
}
