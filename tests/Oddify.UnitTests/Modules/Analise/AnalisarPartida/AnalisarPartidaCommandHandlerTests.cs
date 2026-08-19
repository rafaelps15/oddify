using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Data;
using Oddify.Modules.Analise.Application.Analises.AnalisarPartida;
using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.UnitTests.Modules.Analise.AnalisarPartida;

public sealed class AnalisarPartidaCommandHandlerTests
{
    private readonly ILigaRepository _ligaRepository = Substitute.For<ILigaRepository>();
    private readonly IPartidaRepository _partidaRepository = Substitute.For<IPartidaRepository>();
    private readonly ICotacaoRepository _cotacaoRepository = Substitute.For<ICotacaoRepository>();
    private readonly IAnaliseDePartidaRepository _analiseRepository = Substitute.For<IAnaliseDePartidaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly Guid _partidaId = Guid.NewGuid();
    private readonly Guid _ligaId = Guid.NewGuid();
    private readonly Guid _equipeCasaId = Guid.NewGuid();
    private readonly Guid _equipeVisitanteId = Guid.NewGuid();

    private AnalisarPartidaCommandHandler CriarHandler() => new(
        _ligaRepository, _partidaRepository, _cotacaoRepository, _analiseRepository, _unitOfWork);

    // 10 jogos encerrados em que a equipe marcou `golsFeitos` e sofreu `golsSofridos`, jogando
    // sempre em casa — dá a mesma média (3.0 feitos / 1.0 sofridos, ou o inverso) usada pelo teste
    // antigo, que vinha pronta do IFixturesApi.
    private static List<Partida> CriarJogosRecentes(Guid equipeId, int golsFeitos, int golsSofridos)
    {
        var jogos = new List<Partida>();

        for (int i = 0; i < 10; i++)
        {
            var jogo = Partida.Create(Guid.NewGuid(), Guid.NewGuid(), equipeId, Guid.NewGuid(), DateTime.UtcNow.AddDays(-i));
            jogo.RegistrarResultado(golsFeitos, golsSofridos);
            jogos.Add(jogo);
        }

        return jogos;
    }

    private void ConfigurarDadosBasicos()
    {
        var partida = Partida.Create(_partidaId, _ligaId, _equipeCasaId, _equipeVisitanteId, DateTime.UtcNow);
        _partidaRepository.GetAsync(_partidaId, Arg.Any<CancellationToken>()).Returns(partida);

        var liga = Liga.Create(_ligaId, "Liga de Teste", 2.5m, 1.1m, calibrada: true);
        _ligaRepository.GetAsync(_ligaId, Arg.Any<CancellationToken>()).Returns(liga);

        _partidaRepository.GetRecentesPorEquipeAsync(_equipeCasaId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(CriarJogosRecentes(_equipeCasaId, golsFeitos: 3, golsSofridos: 1));

        _partidaRepository.GetRecentesPorEquipeAsync(_equipeVisitanteId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(CriarJogosRecentes(_equipeVisitanteId, golsFeitos: 1, golsSofridos: 3));
    }

    [Fact]
    public async Task Handle_should_create_and_persist_analise_when_all_group_odds_are_available_from_the_same_casa()
    {
        ConfigurarDadosBasicos();

        _cotacaoRepository.GetMaisRecenteAsync(_partidaId, "vitoria_casa", Arg.Any<CancellationToken>()).Returns(
            Cotacao.Create(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow));

        _cotacaoRepository.GetPorPartidaAsync(_partidaId, Arg.Any<CancellationToken>()).Returns(
        [
            Cotacao.Create(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow),
            Cotacao.Create(Guid.NewGuid(), _partidaId, "empate", 4.2m, "bet365", DateTime.UtcNow),
            Cotacao.Create(Guid.NewGuid(), _partidaId, "vitoria_visitante", 6.0m, "bet365", DateTime.UtcNow)
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

        _cotacaoRepository.GetMaisRecenteAsync(_partidaId, "vitoria_casa", Arg.Any<CancellationToken>()).Returns(
            Cotacao.Create(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow));

        // só a odd do mercado analisado está disponível na bet365 — sem empate/vitoria_visitante não dá para
        // remover a margem com segurança, então a análise deve ser pulada (falha), não calculada com dado incompleto.
        _cotacaoRepository.GetPorPartidaAsync(_partidaId, Arg.Any<CancellationToken>()).Returns(
        [
            Cotacao.Create(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow)
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

        _cotacaoRepository.GetMaisRecenteAsync(_partidaId, "vitoria_casa", Arg.Any<CancellationToken>()).Returns(
            Cotacao.Create(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow));

        // empate/vitoria_visitante só existem numa casa diferente (betano) — não servem para normalizar a
        // margem da cotação escolhida (bet365), então o grupo continua incompleto e a análise deve falhar.
        _cotacaoRepository.GetPorPartidaAsync(_partidaId, Arg.Any<CancellationToken>()).Returns(
        [
            Cotacao.Create(Guid.NewGuid(), _partidaId, "vitoria_casa", 1.5m, "bet365", DateTime.UtcNow),
            Cotacao.Create(Guid.NewGuid(), _partidaId, "empate", 4.0m, "betano", DateTime.UtcNow),
            Cotacao.Create(Guid.NewGuid(), _partidaId, "vitoria_visitante", 5.5m, "betano", DateTime.UtcNow)
        ]);

        var command = new AnalisarPartidaCommand(_partidaId, "vitoria_casa");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be("Analises.DadosIndisponiveis");
    }

    [Fact]
    public async Task Handle_should_return_failure_and_not_persist_when_partida_is_not_found()
    {
        _partidaRepository.GetAsync(_partidaId, Arg.Any<CancellationToken>()).Returns((Partida?)null);

        var command = new AnalisarPartidaCommand(_partidaId, "vitoria_casa");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Code.Should().Be("Analises.DadosIndisponiveis");
        _analiseRepository.DidNotReceive().Insert(Arg.Any<AnaliseDePartida>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
