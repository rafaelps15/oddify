using FluentAssertions;
using NSubstitute;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarApostasDaPartidaEncerrada;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.UnitTests.Modules.Apostas.LiquidarApostasDaPartidaEncerrada;

public sealed class LiquidarApostasDaPartidaEncerradaCommandHandlerTests
{
    private readonly IApostaMultiplaRepository _apostaMultiplaRepository = Substitute.For<IApostaMultiplaRepository>();
    private readonly ICommandsScheduler _commandsScheduler = Substitute.For<ICommandsScheduler>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Guid _usuarioId = Guid.NewGuid();

    private Banca CriarBanca(decimal saldoInicial) =>
        Banca.Create(_usuarioId, "Banca principal", saldoInicial, 0.05m, PerfilDeRisco.Moderado, modoPaperTrading: true, FinalidadeDaBanca.Principal, DateTime.UtcNow);

    private LiquidarApostasDaPartidaEncerradaCommandHandler CriarHandler() =>
        new(_apostaMultiplaRepository, _commandsScheduler, _unitOfWork);

    [Fact]
    public async Task Handle_should_enqueue_LiquidarMultiplaCommand_for_each_pending_aposta()
    {
        var partidaId = Guid.NewGuid();
        Banca banca = CriarBanca(1000m);
        var apostaMultipla1 = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 2.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);
        var apostaMultipla2 = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 3.0m, stake: 20m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);

        _apostaMultiplaRepository.GetPendentesPorPartidaAsync(partidaId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ApostaMultipla>)[apostaMultipla1, apostaMultipla2]);

        Result resultado = await CriarHandler().Handle(new LiquidarApostasDaPartidaEncerradaCommand(partidaId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _commandsScheduler.Received(1).EnqueueAsync(
            Arg.Is<LiquidarMultiplaCommand>(c => c.ApostaMultiplaId == apostaMultipla1.Id && c.UsuarioId == _usuarioId));
        await _commandsScheduler.Received(1).EnqueueAsync(
            Arg.Is<LiquidarMultiplaCommand>(c => c.ApostaMultiplaId == apostaMultipla2.Id && c.UsuarioId == _usuarioId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_return_success_and_not_save_when_there_are_no_pending_apostas()
    {
        var partidaId = Guid.NewGuid();
        _apostaMultiplaRepository.GetPendentesPorPartidaAsync(partidaId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ApostaMultipla>)[]);

        Result resultado = await CriarHandler().Handle(new LiquidarApostasDaPartidaEncerradaCommand(partidaId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _commandsScheduler.DidNotReceive().EnqueueAsync(Arg.Any<LiquidarMultiplaCommand>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
