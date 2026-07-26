using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.PublicApi;
using Oddify.Modules.Apostas.Application.Abstractions.Data;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.PernasDeAposta;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.UnitTests.Modules.Apostas.LiquidarMultipla;

public sealed class LiquidarMultiplaCommandHandlerTests
{
    private readonly IApostaMultiplaRepository _apostaMultiplaRepository = Substitute.For<IApostaMultiplaRepository>();
    private readonly IPernaDeApostaRepository _pernaDeApostaRepository = Substitute.For<IPernaDeApostaRepository>();
    private readonly IBancaRepository _bancaRepository = Substitute.For<IBancaRepository>();
    private readonly IFixturesApi _fixturesApi = Substitute.For<IFixturesApi>();
    private readonly IAnaliseApi _analiseApi = Substitute.For<IAnaliseApi>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private LiquidarMultiplaCommandHandler CriarHandler() =>
        new(_apostaMultiplaRepository, _pernaDeApostaRepository, _bancaRepository, _fixturesApi, _analiseApi, _unitOfWork);

    private static PartidaResponse PartidaEncerrada(Guid id, int golsCasa, int golsVisitante) =>
        new(id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "Encerrada", golsCasa, golsVisitante);

    [Fact]
    public async Task Handle_should_mark_multipla_ganha_when_all_pernas_win()
    {
        var banca = Banca.Create(1000m, modoPaperTrading: true);
        var apostaMultipla = ApostaMultipla.Create(banca.Id, oddCombinada: 4.0m, stake: 50m, DateTime.UtcNow);

        var partida1 = Guid.NewGuid();
        var partida2 = Guid.NewGuid();
        var perna1 = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), partida1, "vitoria_casa", 2.0m);
        var perna2 = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), partida2, "vitoria_casa", 2.0m);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna1, perna2]);

        _fixturesApi.ObterPartidaAsync(partida1, Arg.Any<CancellationToken>()).Returns(PartidaEncerrada(partida1, 2, 0));
        _fixturesApi.ObterPartidaAsync(partida2, Arg.Any<CancellationToken>()).Returns(PartidaEncerrada(partida2, 1, 0));

        _analiseApi.ResolverMercado("vitoria_casa", Arg.Any<int>(), Arg.Any<int>()).Returns(true);

        _bancaRepository.GetAsync(banca.Id, Arg.Any<CancellationToken>()).Returns(banca);

        Result resultado = await CriarHandler().Handle(new LiquidarMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Ganha);
        banca.SaldoAtual.Should().Be(1000m + 50m * (4.0m - 1m));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_mark_multipla_perdida_when_one_perna_loses()
    {
        var banca = Banca.Create(1000m, modoPaperTrading: true);
        var apostaMultipla = ApostaMultipla.Create(banca.Id, oddCombinada: 4.0m, stake: 50m, DateTime.UtcNow);

        var partida1 = Guid.NewGuid();
        var partida2 = Guid.NewGuid();
        var perna1 = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), partida1, "vitoria_casa", 2.0m);
        var perna2 = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), partida2, "vitoria_casa", 2.0m);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna1, perna2]);

        _fixturesApi.ObterPartidaAsync(partida1, Arg.Any<CancellationToken>()).Returns(PartidaEncerrada(partida1, 2, 0));
        _fixturesApi.ObterPartidaAsync(partida2, Arg.Any<CancellationToken>()).Returns(PartidaEncerrada(partida2, 0, 1));

        _analiseApi.ResolverMercado("vitoria_casa", 2, 0).Returns(true);
        _analiseApi.ResolverMercado("vitoria_casa", 0, 1).Returns(false);

        _bancaRepository.GetAsync(banca.Id, Arg.Any<CancellationToken>()).Returns(banca);

        Result resultado = await CriarHandler().Handle(new LiquidarMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Perdida);
        banca.SaldoAtual.Should().Be(1000m - 50m);
    }

    [Fact]
    public async Task Handle_should_fail_when_a_partida_is_not_yet_encerrada()
    {
        var banca = Banca.Create(1000m, modoPaperTrading: true);
        var apostaMultipla = ApostaMultipla.Create(banca.Id, oddCombinada: 2.0m, stake: 50m, DateTime.UtcNow);

        var partidaId = Guid.NewGuid();
        var perna = PernaDeAposta.Create(apostaMultipla.Id, Guid.NewGuid(), partidaId, "vitoria_casa", 2.0m);

        _apostaMultiplaRepository.GetAsync(apostaMultipla.Id, Arg.Any<CancellationToken>()).Returns(apostaMultipla);
        _pernaDeApostaRepository.GetPorApostaMultiplaAsync(apostaMultipla.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<PernaDeAposta>)[perna]);

        _fixturesApi.ObterPartidaAsync(partidaId, Arg.Any<CancellationToken>())
            .Returns(new PartidaResponse(partidaId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, "Agendada", null, null));

        Result resultado = await CriarHandler().Handle(new LiquidarMultiplaCommand(apostaMultipla.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        apostaMultipla.Resultado.Should().Be(ResultadoDaAposta.Pendente);
    }
}
