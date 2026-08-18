using FluentAssertions;
using MediatR;
using NSubstitute;
using Oddify.Common.Application.Exceptions;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.IntegrationEvents;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.RegistrarAnaliseDisponivelParaAposta;
using Oddify.Modules.Apostas.Presentation.IntegrationEvents;

namespace Oddify.UnitTests.Modules.Apostas.IntegrationEvents;

public sealed class AnaliseConfirmadaIntegrationEventConsumerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    private AnaliseConfirmadaIntegrationEventConsumer CriarConsumer() => new(_sender);

    [Fact]
    public async Task Handle_should_dispatch_registrar_analise_disponivel_command()
    {
        var evento = new AnaliseConfirmadaIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 1.85m, 0.62m, reduzida: false);
        _sender.Send(Arg.Any<RegistrarAnaliseDisponivelParaApostaCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        await CriarConsumer().Handle(evento, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<RegistrarAnaliseDisponivelParaApostaCommand>(c =>
                c.AnaliseId == evento.AnaliseId &&
                c.PartidaId == evento.PartidaId &&
                c.Mercado == evento.Mercado &&
                c.OddDeMercado == evento.OddDeMercado &&
                c.ProbabilidadeConfirmada == evento.ProbabilidadeConfirmada &&
                c.Reduzida == evento.Reduzida),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_throw_when_command_fails()
    {
        var evento = new AnaliseConfirmadaIntegrationEvent(
            Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 1.85m, 0.62m, reduzida: false);
        _sender.Send(Arg.Any<RegistrarAnaliseDisponivelParaApostaCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Failure("AnalisesDisponiveis.Falha", "falha simulada")));

        Func<Task> act = () => CriarConsumer().Handle(evento, CancellationToken.None);

        await act.Should().ThrowAsync<OddifyException>();
    }
}
