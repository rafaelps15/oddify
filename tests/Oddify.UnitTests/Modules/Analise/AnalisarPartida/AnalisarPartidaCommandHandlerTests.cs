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

    private void ConfigurarDependenciasFelizes(decimal odd, bool ligaCalibrada = true, int amostra = 10)
    {
        _fixturesApi.ObterPartidaAsync(_partidaId, Arg.Any<CancellationToken>())
            .Returns(new PartidaResponse(_partidaId, _ligaId, _equipeCasaId, _equipeVisitanteId, DateTime.UtcNow, "Agendada", null, null));

        _fixturesApi.ObterLigaAsync(_ligaId, Arg.Any<CancellationToken>())
            .Returns(new LigaResponse(_ligaId, "Liga de Teste", 2.5m, 1.1m, ligaCalibrada));

        // Time da casa ataca bem acima da média e o visitante defende mal (e vice-versa) para dar
        // uma margem folgada de vantagem, evitando que a correção de Dixon-Coles ou arredondamento
        // decimal empurrem o resultado para o lado errado do limiar de 4%.
        _fixturesApi.ObterHistoricoRecenteAsync(_equipeCasaId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new HistoricoDeEquipeResponse(amostra, 3.0m, 1.0m));

        _fixturesApi.ObterHistoricoRecenteAsync(_equipeVisitanteId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new HistoricoDeEquipeResponse(amostra, 1.0m, 3.0m));

        _fixturesApi.ObterCotacaoMaisRecenteAsync(_partidaId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CotacaoResponse(Guid.NewGuid(), _partidaId, "vitoria_casa", odd, "casa-de-apostas", DateTime.UtcNow));
    }

    [Fact]
    public async Task Handle_should_approve_analysis_when_advantage_and_odd_are_within_filter_range()
    {
        // Time da casa ataca mais e defende melhor que a média -> lambda casa alto ->
        // probabilidade de vitória da casa bem superior à implícita de uma odd de 1.5.
        ConfigurarDependenciasFelizes(odd: 1.5m);

        var command = new AnalisarPartidaCommand(_partidaId, "vitoria_casa");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();

        _analiseRepository.Received(1).Insert(Arg.Is<AnaliseDePartida>(a =>
            a.PartidaId == _partidaId &&
            a.Mercado == "vitoria_casa" &&
            a.AprovadaNoFiltro));

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_reject_analysis_with_motivo_when_odd_is_outside_filter_range()
    {
        ConfigurarDependenciasFelizes(odd: 3.0m);

        var command = new AnalisarPartidaCommand(_partidaId, "vitoria_casa");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();

        _analiseRepository.Received(1).Insert(Arg.Is<AnaliseDePartida>(a =>
            !a.AprovadaNoFiltro && a.MotivoDoDescarte != null && a.MotivoDoDescarte.Contains("Odd")));
    }

    [Fact]
    public async Task Handle_should_return_failure_when_partida_not_found()
    {
        _fixturesApi.ObterPartidaAsync(_partidaId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<PartidaResponse>(Error.NotFound("Partidas.NotFound", "não encontrada")));

        var command = new AnalisarPartidaCommand(_partidaId, "vitoria_casa");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        _analiseRepository.DidNotReceive().Insert(Arg.Any<AnaliseDePartida>());
    }
}
