using FluentAssertions;
using MediatR;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarApostasDaPartidaEncerrada;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.UnitTests.Modules.Apostas.LiquidarApostasDaPartidaEncerrada;

public sealed class LiquidarApostasDaPartidaEncerradaCommandHandlerTests
{
    private readonly IApostaMultiplaRepository _apostaMultiplaRepository = Substitute.For<IApostaMultiplaRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly Guid _usuarioId = Guid.NewGuid();

    private Banca CriarBanca(decimal saldoInicial) =>
        Banca.Create(_usuarioId, "Banca principal", saldoInicial, 0.05m, PerfilDeRisco.Moderado, modoPaperTrading: true, FinalidadeDaBanca.Principal, DateTime.UtcNow);

    private LiquidarApostasDaPartidaEncerradaCommandHandler CriarHandler() => new(_apostaMultiplaRepository, _sender);

    [Fact]
    public async Task Handle_should_resend_LiquidarMultiplaCommand_for_each_pending_aposta()
    {
        var partidaId = Guid.NewGuid();
        Banca banca = CriarBanca(1000m);
        var apostaMultipla1 = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 2.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);
        var apostaMultipla2 = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 3.0m, stake: 20m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);

        _apostaMultiplaRepository.GetPendentesPorPartidaAsync(partidaId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ApostaMultipla>)[apostaMultipla1, apostaMultipla2]);

        _sender.Send(Arg.Any<LiquidarMultiplaCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        Result resultado = await CriarHandler().Handle(new LiquidarApostasDaPartidaEncerradaCommand(partidaId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _sender.Received(1).Send(
            Arg.Is<LiquidarMultiplaCommand>(c => c.ApostaMultiplaId == apostaMultipla1.Id && c.UsuarioId == _usuarioId),
            Arg.Any<CancellationToken>());
        await _sender.Received(1).Send(
            Arg.Is<LiquidarMultiplaCommand>(c => c.ApostaMultiplaId == apostaMultipla2.Id && c.UsuarioId == _usuarioId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_keep_going_when_one_command_fails()
    {
        var partidaId = Guid.NewGuid();
        Banca banca = CriarBanca(1000m);
        var apostaMultipla1 = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 2.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);
        var apostaMultipla2 = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 3.0m, stake: 20m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);

        _apostaMultiplaRepository.GetPendentesPorPartidaAsync(partidaId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ApostaMultipla>)[apostaMultipla1, apostaMultipla2]);

        // Uma múltipla com outra perna ainda pendente em partida diferente falha dentro do
        // LiquidarMultiplaCommandHandler (partida não encerrada) — não deve interromper o laço nem
        // fazer o handler de lote propagar a falha pro consumer.
        var erro = Error.Problem("ApostasMultiplas.PartidaNaoEncerrada", "A partida ainda não foi encerrada");
        _sender.Send(Arg.Is<LiquidarMultiplaCommand>(c => c.ApostaMultiplaId == apostaMultipla1.Id), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(erro));
        _sender.Send(Arg.Is<LiquidarMultiplaCommand>(c => c.ApostaMultiplaId == apostaMultipla2.Id), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        Result resultado = await CriarHandler().Handle(new LiquidarApostasDaPartidaEncerradaCommand(partidaId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _sender.Received(1).Send(
            Arg.Is<LiquidarMultiplaCommand>(c => c.ApostaMultiplaId == apostaMultipla2.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_return_success_and_not_send_anything_when_there_are_no_pending_apostas()
    {
        var partidaId = Guid.NewGuid();
        _apostaMultiplaRepository.GetPendentesPorPartidaAsync(partidaId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ApostaMultipla>)[]);

        Result resultado = await CriarHandler().Handle(new LiquidarApostasDaPartidaEncerradaCommand(partidaId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _sender.DidNotReceive().Send(Arg.Any<LiquidarMultiplaCommand>(), Arg.Any<CancellationToken>());
    }
}
