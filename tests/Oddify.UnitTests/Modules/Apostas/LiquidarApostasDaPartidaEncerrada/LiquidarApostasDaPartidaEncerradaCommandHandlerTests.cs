using FluentAssertions;
using MediatR;
using NSubstitute;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.ApostasMultiplas;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetResultadosDasPernas;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarApostasDaPartidaEncerrada;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.UnitTests.Modules.Apostas.LiquidarApostasDaPartidaEncerrada;

public sealed class LiquidarApostasDaPartidaEncerradaCommandHandlerTests
{
    private readonly IApostaMultiplaRepository _apostaMultiplaRepository = Substitute.For<IApostaMultiplaRepository>();
    private readonly IPernaDeApostaRepository _pernaDeApostaRepository = Substitute.For<IPernaDeApostaRepository>();
    private readonly IBancaRepository _bancaRepository = Substitute.For<IBancaRepository>();
    private readonly IMovimentacaoDaBancaRepository _movimentacaoDaBancaRepository = Substitute.For<IMovimentacaoDaBancaRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly Guid _usuarioId = Guid.NewGuid();

    private Banca CriarBanca(decimal saldoInicial) =>
        Banca.Create(_usuarioId, "Banca principal", saldoInicial, 0.05m, PerfilDeRisco.Moderado, modoPaperTrading: true, FinalidadeDaBanca.Principal, DateTime.UtcNow);

    private LiquidarApostasDaPartidaEncerradaCommandHandler CriarHandler()
    {
        var liquidacaoService = new ApostaMultiplaLiquidacaoService(
            _pernaDeApostaRepository, _bancaRepository, _movimentacaoDaBancaRepository, _sender, _dateTimeProvider);
        return new(_apostaMultiplaRepository, liquidacaoService, _unitOfWork);
    }

    [Fact]
    public async Task Handle_should_liquidate_pending_apostas_for_the_partida()
    {
        var partidaId = Guid.NewGuid();
        Banca banca = CriarBanca(1000m);
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 2.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);
        var perna = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), partidaId, "vitoria_casa", 2.0m);

        _apostaMultiplaRepository.GetPendentesPorPartidaAsync(partidaId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ApostaMultipla>)[apostaMultipla]);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna]);
        _sender.Send(Arg.Any<GetResultadosDasPernasQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyDictionary<Guid, bool>>(new Dictionary<Guid, bool> { [perna.Id] = true }));
        _bancaRepository.GetAsync(banca.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(banca);

        Result resultado = await CriarHandler().Handle(new LiquidarApostasDaPartidaEncerradaCommand(partidaId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Ganha);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_skip_aposta_and_keep_it_pending_when_another_perna_is_not_ready()
    {
        var partidaId = Guid.NewGuid();
        Banca banca = CriarBanca(1000m);
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 4.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);
        var perna1 = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), partidaId, "vitoria_casa", 2.0m);
        var perna2 = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 2.0m);

        _apostaMultiplaRepository.GetPendentesPorPartidaAsync(partidaId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ApostaMultipla>)[apostaMultipla]);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna1, perna2]);

        var erro = Error.Problem("ApostasMultiplas.PartidaNaoEncerrada", "A partida ainda não foi encerrada");
        _sender.Send(Arg.Any<GetResultadosDasPernasQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyDictionary<Guid, bool>>(erro));

        Result resultado = await CriarHandler().Handle(new LiquidarApostasDaPartidaEncerradaCommand(partidaId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Pendente);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_return_success_and_not_save_when_there_are_no_pending_apostas()
    {
        var partidaId = Guid.NewGuid();
        _apostaMultiplaRepository.GetPendentesPorPartidaAsync(partidaId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<ApostaMultipla>)[]);

        Result resultado = await CriarHandler().Handle(new LiquidarApostasDaPartidaEncerradaCommand(partidaId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
