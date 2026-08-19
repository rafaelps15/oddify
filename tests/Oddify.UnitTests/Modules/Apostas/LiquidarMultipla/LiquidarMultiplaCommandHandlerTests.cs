using FluentAssertions;
using MediatR;
using NSubstitute;
using Oddify.Common.Application.Clock;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetResultadosDasPernas;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;

namespace Oddify.UnitTests.Modules.Apostas.LiquidarMultipla;

public sealed class LiquidarMultiplaCommandHandlerTests
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

    private LiquidarMultiplaCommandHandler CriarHandler() => new(
        _apostaMultiplaRepository,
        _pernaDeApostaRepository,
        _bancaRepository,
        _movimentacaoDaBancaRepository,
        _sender,
        _dateTimeProvider,
        _unitOfWork);

    private void ConfigurarResultados(IReadOnlyDictionary<Guid, bool> resultadosPorPernaId)
    {
        _sender.Send(Arg.Any<GetResultadosDasPernasQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyDictionary<Guid, bool>>(resultadosPorPernaId));
    }

    [Fact]
    public async Task Handle_should_mark_multipla_ganha_when_all_pernas_win()
    {
        Banca banca = CriarBanca(1000m);
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 4.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);

        var perna1 = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 2.0m);
        var perna2 = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 2.0m);

        _apostaMultiplaRepository.GetByIdAsync(apostaMultipla.Id, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna1, perna2]);

        ConfigurarResultados(new Dictionary<Guid, bool> { [perna1.Id] = true, [perna2.Id] = true });

        _bancaRepository.GetAsync(banca.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(banca);

        Result resultado = await CriarHandler().Handle(new LiquidarMultiplaCommand(apostaMultipla.Id, _usuarioId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Ganha);
        banca.SaldoAtual.Should().Be(1000m + 50m * (4.0m - 1m));
        _movimentacaoDaBancaRepository.Received(1).Insert(Arg.Is<MovimentacaoDaBanca>(m =>
            m.BancaId == banca.Id
            && m.Tipo == TipoDeMovimentacao.Liquidacao
            && m.Valor == 50m * (4.0m - 1m)
            && m.SaldoAposMovimentacao == banca.SaldoAtual
            && m.ApostaMultiplaId == apostaMultipla.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_mark_multipla_perdida_when_one_perna_loses()
    {
        Banca banca = CriarBanca(1000m);
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 4.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);

        var perna1 = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 2.0m);
        var perna2 = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 2.0m);

        _apostaMultiplaRepository.GetByIdAsync(apostaMultipla.Id, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna1, perna2]);

        ConfigurarResultados(new Dictionary<Guid, bool> { [perna1.Id] = true, [perna2.Id] = false });

        _bancaRepository.GetAsync(banca.Id, _usuarioId, Arg.Any<CancellationToken>()).Returns(banca);

        Result resultado = await CriarHandler().Handle(new LiquidarMultiplaCommand(apostaMultipla.Id, _usuarioId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Perdida);
        banca.SaldoAtual.Should().Be(1000m - 50m);
    }

    [Fact]
    public async Task Handle_should_fail_when_query_fails()
    {
        Banca banca = CriarBanca(1000m);
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 2.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);

        var perna = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", 2.0m);

        _apostaMultiplaRepository.GetByIdAsync(apostaMultipla.Id, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna]);

        var erro = Error.Problem("ApostasMultiplas.PartidaNaoEncerrada", "A partida ainda não foi encerrada");
        _sender.Send(Arg.Any<GetResultadosDasPernasQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyDictionary<Guid, bool>>(erro));

        Result resultado = await CriarHandler().Handle(new LiquidarMultiplaCommand(apostaMultipla.Id, _usuarioId), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(erro);
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Pendente);
    }

    [Fact]
    public async Task Handle_should_return_not_found_when_aposta_belongs_to_a_different_usuario()
    {
        Banca banca = CriarBanca(1000m);
        var apostaMultipla = ApostaMultipla.Create(
            _usuarioId, banca.Id, oddCombinada: 2.0m, stake: 50m, OrigemDaAposta.ManualEntry, descricao: null, passoDaJornadaId: null, DateTime.UtcNow);

        _apostaMultiplaRepository.GetByIdAsync(apostaMultipla.Id, Arg.Any<CancellationToken>()).Returns(apostaMultipla);

        Result resultado = await CriarHandler().Handle(new LiquidarMultiplaCommand(apostaMultipla.Id, Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ApostaMultiplaErrors.NotFound(apostaMultipla.Id));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_return_not_found_when_aposta_does_not_exist()
    {
        var apostaMultiplaId = Guid.NewGuid();
        _apostaMultiplaRepository.GetByIdAsync(apostaMultiplaId, Arg.Any<CancellationToken>()).Returns((ApostaMultipla?)null);

        Result resultado = await CriarHandler().Handle(new LiquidarMultiplaCommand(apostaMultiplaId, _usuarioId), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(ApostaMultiplaErrors.NotFound(apostaMultiplaId));
    }
}
