using FluentAssertions;
using NSubstitute;
using Oddify.Modules.Analise.Application.Abstractions.Fixtures;
using Oddify.Modules.Analise.Application.Calculo;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.UnitTests.Modules.Analise.Abstractions;

public sealed class AnaliseDePartidaDadosServiceTests
{
    private readonly IFixturesApi _fixturesApi = Substitute.For<IFixturesApi>();
    private readonly Guid _partidaId = Guid.NewGuid();
    private readonly Guid _ligaId = Guid.NewGuid();
    private readonly Guid _equipeCasaId = Guid.NewGuid();
    private readonly Guid _equipeVisitanteId = Guid.NewGuid();

    private AnaliseDePartidaDadosService CriarService() => new(_fixturesApi);

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
    public async Task ObterAsync_should_return_analise_when_all_group_odds_are_available_from_the_same_casa()
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

        AnaliseCalculada? resultado = await CriarService().ObterAsync(_partidaId, "vitoria_casa");

        resultado.Should().NotBeNull();
    }

    [Fact]
    public async Task ObterAsync_should_return_null_when_a_sibling_market_odd_is_missing_from_the_same_casa()
    {
        ConfigurarDadosBasicos();

        _fixturesApi.ObterCotacaoMaisRecenteAsync(_partidaId, "vitoria_casa", Arg.Any<CancellationToken>()).Returns(
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow));

        // só a odd do mercado analisado está disponível na bet365 — sem empate/vitoria_visitante não dá para
        // remover a margem com segurança, então a análise deve ser pulada (null), não calculada com dado incompleto.
        _fixturesApi.ObterCotacoesPorPartidaAsync(_partidaId, Arg.Any<CancellationToken>()).Returns(
        [
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow)
        ]);

        AnaliseCalculada? resultado = await CriarService().ObterAsync(_partidaId, "vitoria_casa");

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task ObterAsync_should_ignore_sibling_odds_from_a_different_casa()
    {
        ConfigurarDadosBasicos();

        _fixturesApi.ObterCotacaoMaisRecenteAsync(_partidaId, "vitoria_casa", Arg.Any<CancellationToken>()).Returns(
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow));

        // empate/vitoria_visitante só existem numa casa diferente (betano) — não servem para normalizar a
        // margem da cotação escolhida (bet365), então o grupo continua incompleto e a análise deve ser pulada.
        _fixturesApi.ObterCotacoesPorPartidaAsync(_partidaId, Arg.Any<CancellationToken>()).Returns(
        [
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow),
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "empate", 4.0m, "betano", DateTime.UtcNow),
            new CotacaoResponse(Guid.NewGuid(), _partidaId, "vitoria_visitante", 5.5m, "betano", DateTime.UtcNow)
        ]);

        AnaliseCalculada? resultado = await CriarService().ObterAsync(_partidaId, "vitoria_casa");

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task ObterAsync_should_return_null_when_partida_is_not_found()
    {
        _fixturesApi.ObterPartidaAsync(_partidaId, Arg.Any<CancellationToken>()).Returns((PartidaResponse?)null);

        AnaliseCalculada? resultado = await CriarService().ObterAsync(_partidaId, "vitoria_casa");

        resultado.Should().BeNull();
    }
}
