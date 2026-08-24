using FluentAssertions;
using MediatR;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Analises.AnalisarPartida;
using Oddify.Modules.Analise.Application.Fixtures.RegistrarCotacao;
using Oddify.Modules.Analise.Presentation.IntegrationEvents;
using Oddify.Modules.Fixtures.IntegrationEvents;

namespace Oddify.UnitTests.Modules.Analise.IntegrationEvents;

public sealed class CotacaoColetadaIntegrationEventConsumerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();

    private CotacaoColetadaIntegrationEventConsumer CriarConsumer() => new(_sender);

    private static CotacaoColetadaIntegrationEvent CriarEvento() => new(
        Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 1.85m, "bet365");

    [Fact]
    public async Task Handle_should_dispatch_registrar_cotacao_command_with_correct_fields()
    {
        CotacaoColetadaIntegrationEvent evento = CriarEvento();
        _sender.Send(Arg.Any<RegistrarCotacaoCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        _sender.Send(Arg.Any<AnalisarPartidaCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));

        await CriarConsumer().Handle(evento, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<RegistrarCotacaoCommand>(c =>
                c.CotacaoId == evento.CotacaoId &&
                c.PartidaId == evento.PartidaId &&
                c.Mercado == evento.Mercado &&
                c.Odd == evento.Odd &&
                c.Casa == evento.Casa &&
                c.ColetadaEmUtc == evento.OccurredOnUtc),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_dispatch_analisar_partida_command_after_registrar_cotacao_succeeds()
    {
        CotacaoColetadaIntegrationEvent evento = CriarEvento();
        _sender.Send(Arg.Any<RegistrarCotacaoCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        _sender.Send(Arg.Any<AnalisarPartidaCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));

        await CriarConsumer().Handle(evento, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<AnalisarPartidaCommand>(c => c.PartidaId == evento.PartidaId && c.Mercado == evento.Mercado),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_not_throw_and_not_dispatch_analisar_partida_when_registrar_cotacao_fails()
    {
        CotacaoColetadaIntegrationEvent evento = CriarEvento();
        _sender.Send(Arg.Any<RegistrarCotacaoCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Failure("Cotacoes.Falha", "falha simulada")));

        Func<Task> act = () => CriarConsumer().Handle(evento, CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _sender.DidNotReceive().Send(Arg.Any<AnalisarPartidaCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_not_throw_when_analisar_partida_fails()
    {
        CotacaoColetadaIntegrationEvent evento = CriarEvento();
        _sender.Send(Arg.Any<RegistrarCotacaoCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        _sender.Send(Arg.Any<AnalisarPartidaCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Guid>(Error.Failure("Analises.DadosIndisponiveis", "falha simulada")));

        Func<Task> act = () => CriarConsumer().Handle(evento, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
