using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Analises.AnalisarPartida;
using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.UnitTests.Modules.Analise.AnalisarPartida;

public sealed class AnalisarPartidaCommandHandlerTests
{
    private readonly IFixturesApi _fixturesApi = Substitute.For<IFixturesApi>();
    private readonly IAnaliseDePartidaRepository _analiseRepository = Substitute.For<IAnaliseDePartidaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly Guid _partidaId = Guid.NewGuid();
    private readonly Guid _ligaId = Guid.NewGuid();
    private readonly Guid _equipeCasaId = Guid.NewGuid();
    private readonly Guid _equipeVisitanteId = Guid.NewGuid();

    private AnalisarPartidaCommandHandler CriarHandler() => new(_fixturesApi, _analiseRepository, _unitOfWork);

    private void ConfigurarDadosBasicos()
    {
        _fixturesApi.ObterPartidaAsync(_partidaId, Arg.Any<CancellationToken>()).Returns(
            new PartidaResponse(_partidaId, _ligaId, _equipeCasaId, _equipeVisitanteId, DateTime.UtcNow, "Agendada", null, null));

        _fixturesApi.ObterLigaAsync(_ligaId, Arg.Any<CancellationToken>()).Returns(
            new LigaResponse(_ligaId, "Liga de Teste", 2.5m, 1.1m, Calibrada: true));

        _fixturesApi.ObterHistoricoRecenteAsync(_equipeCasaId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(
            new HistoricoDeEquipeResponse(10, 3.0m, 1.0m));

        _fixturesApi.ObterHistoricoRecenteAsync(_equipeVisitanteId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(
            new HistoricoDeEquipeResponse(10, 1.0m, 3.0m));
    }

    [Fact]
    public async Task Handle_should_create_and_persist_analise_when_all_group_odds_are_available_from_the_same_casa()
    {
        ConfigurarDadosBasicos();

        _fixturesApi.ObterCotacaoMaisRecenteAsync(_partidaId, "vitoria_casa", Arg.Any<CancellationToken>()).Returns(
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow));

        _fixturesApi.ObterCotacoesPorPartidaAsync(_partidaId, Arg.Any<CancellationToken>()).Returns(
        [
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow),
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "empate", 4.2m, "bet365", DateTime.UtcNow),
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "vitoria_visitante", 6.0m, "bet365", DateTime.UtcNow)
        ]);

        var command = new AnalisarPartidaCommand(_partidaId, "vitoria_casa");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _analiseRepository.Received(1).Insert(Arg.Is<AnaliseDePartida>(a =>
            a.PartidaId == _partidaId && a.Mercado == "vitoria_casa"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_a_sibling_market_odd_is_missing_from_the_same_casa()
    {
        ConfigurarDadosBasicos();

        _fixturesApi.ObterCotacaoMaisRecenteAsync(_partidaId, "vitoria_casa", Arg.Any<CancellationToken>()).Returns(
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow));

        // só a odd do mercado analisado está disponível na bet365 — sem empate/vitoria_visitante não dá para
        // remover a margem com segurança, então a análise deve ser pulada (falha), não calculada com dado incompleto.
        _fixturesApi.ObterCotacoesPorPartidaAsync(_partidaId, Arg.Any<CancellationToken>()).Returns(
        [
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow)
        ]);

        var command = new AnalisarPartidaCommand(_partidaId, "vitoria_casa");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be("Analises.DadosIndisponiveis");
        _analiseRepository.DidNotReceive().Insert(Arg.Any<AnaliseDePartida>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_sibling_odds_are_only_available_from_a_different_casa()
    {
        ConfigurarDadosBasicos();

        _fixturesApi.ObterCotacaoMaisRecenteAsync(_partidaId, "vitoria_casa", Arg.Any<CancellationToken>()).Returns(
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow));

        // empate/vitoria_visitante só existem numa casa diferente (betano) — não servem para normalizar a
        // margem da cotação escolhida (bet365), então o grupo continua incompleto e a análise deve falhar.
        _fixturesApi.ObterCotacoesPorPartidaAsync(_partidaId, Arg.Any<CancellationToken>()).Returns(
        [
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow),
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "empate", 4.0m, "betano", DateTime.UtcNow),
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "vitoria_visitante", 5.5m, "betano", DateTime.UtcNow)
        ]);

        var command = new AnalisarPartidaCommand(_partidaId, "vitoria_casa");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be("Analises.DadosIndisponiveis");
    }

    [Fact]
    public async Task Handle_should_return_failure_and_not_persist_when_partida_is_not_found()
    {
        _fixturesApi.ObterPartidaAsync(_partidaId, Arg.Any<CancellationToken>()).Returns((PartidaResponse?)null);

        var command = new AnalisarPartidaCommand(_partidaId, "vitoria_casa");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be("Analises.DadosIndisponiveis");
        _analiseRepository.DidNotReceive().Insert(Arg.Any<AnaliseDePartida>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
