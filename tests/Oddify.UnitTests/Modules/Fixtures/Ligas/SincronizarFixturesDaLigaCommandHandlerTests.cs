using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;
using Oddify.Modules.Fixtures.Application.Ligas.SincronizarFixturesDaLiga;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.UnitTests.Modules.Fixtures.Ligas;

public sealed class SincronizarFixturesDaLigaCommandHandlerTests
{
    private readonly ILigaConfiguradaRepository _ligaRepository = Substitute.For<ILigaConfiguradaRepository>();
    private readonly IEquipeRepository _equipeRepository = Substitute.For<IEquipeRepository>();
    private readonly IPartidaRepository _partidaRepository = Substitute.For<IPartidaRepository>();
    private readonly IApiFootballClient _apiFootballClient = Substitute.For<IApiFootballClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static readonly LigaConfigurada Liga = LigaConfigurada.Create("liga-1", "Premier League", 2.5m, 1.1m);

    private SincronizarFixturesDaLigaCommandHandler CriarHandler() =>
        new(_ligaRepository, _equipeRepository, _partidaRepository, _apiFootballClient, _unitOfWork);

    private static FixtureExternoDto CriarFixture(bool encerrada = false, int? golsCasa = null, int? golsVisitante = null) =>
        new("fixture-1", "time-casa", "Flamengo", "time-visitante", "Palmeiras", DateTime.UtcNow.AddDays(1), encerrada, golsCasa, golsVisitante);

    [Fact]
    public async Task Handle_should_fail_when_liga_not_found()
    {
        _ligaRepository.GetAsync(Liga.Id, Arg.Any<CancellationToken>()).Returns((LigaConfigurada?)null);

        var command = new SincronizarFixturesDaLigaCommand(Liga.Id, 2026);

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(LigaConfiguradaErrors.NotFound(Liga.Id));
    }

    [Fact]
    public async Task Handle_should_create_equipes_and_partida_when_fixture_is_new()
    {
        _ligaRepository.GetAsync(Liga.Id, Arg.Any<CancellationToken>()).Returns(Liga);
        _apiFootballClient.GetFixturesAsync(Liga.IdExterno, 2026, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<FixtureExternoDto>>([CriarFixture()]));
        _equipeRepository.GetByIdExternoAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Equipe?)null);
        _partidaRepository.GetByIdExternoAsync("fixture-1", Arg.Any<CancellationToken>()).Returns((Partida?)null);

        var command = new SincronizarFixturesDaLigaCommand(Liga.Id, 2026);

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _equipeRepository.Received(1).Insert(Arg.Is<Equipe>(e => e.IdExterno == "time-casa" && e.Nome == "Flamengo"));
        _equipeRepository.Received(1).Insert(Arg.Is<Equipe>(e => e.IdExterno == "time-visitante" && e.Nome == "Palmeiras"));
        _partidaRepository.Received(1).Insert(Arg.Is<Partida>(p => p.IdExterno == "fixture-1"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_not_duplicate_equipes_or_partida_when_they_already_exist()
    {
        var equipeCasa = Equipe.Create("time-casa", "Flamengo", Liga.Id);
        var equipeVisitante = Equipe.Create("time-visitante", "Palmeiras", Liga.Id);
        var partidaExistente = Partida.Create("fixture-1", Liga.Id, equipeCasa.Id, equipeVisitante.Id, DateTime.UtcNow.AddDays(1));

        _ligaRepository.GetAsync(Liga.Id, Arg.Any<CancellationToken>()).Returns(Liga);
        _apiFootballClient.GetFixturesAsync(Liga.IdExterno, 2026, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<FixtureExternoDto>>([CriarFixture()]));
        _equipeRepository.GetByIdExternoAsync("time-casa", Arg.Any<CancellationToken>()).Returns(equipeCasa);
        _equipeRepository.GetByIdExternoAsync("time-visitante", Arg.Any<CancellationToken>()).Returns(equipeVisitante);
        _partidaRepository.GetByIdExternoAsync("fixture-1", Arg.Any<CancellationToken>()).Returns(partidaExistente);

        var command = new SincronizarFixturesDaLigaCommand(Liga.Id, 2026);

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _equipeRepository.DidNotReceive().Insert(Arg.Any<Equipe>());
        _partidaRepository.DidNotReceive().Insert(Arg.Any<Partida>());
    }

    [Fact]
    public async Task Handle_should_registrar_resultado_when_fixture_is_encerrada_and_partida_still_agendada()
    {
        var equipeCasa = Equipe.Create("time-casa", "Flamengo", Liga.Id);
        var equipeVisitante = Equipe.Create("time-visitante", "Palmeiras", Liga.Id);
        var partidaExistente = Partida.Create("fixture-1", Liga.Id, equipeCasa.Id, equipeVisitante.Id, DateTime.UtcNow.AddDays(-1));

        _ligaRepository.GetAsync(Liga.Id, Arg.Any<CancellationToken>()).Returns(Liga);
        _apiFootballClient.GetFixturesAsync(Liga.IdExterno, 2026, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<FixtureExternoDto>>([CriarFixture(encerrada: true, golsCasa: 2, golsVisitante: 1)]));
        _equipeRepository.GetByIdExternoAsync("time-casa", Arg.Any<CancellationToken>()).Returns(equipeCasa);
        _equipeRepository.GetByIdExternoAsync("time-visitante", Arg.Any<CancellationToken>()).Returns(equipeVisitante);
        _partidaRepository.GetByIdExternoAsync("fixture-1", Arg.Any<CancellationToken>()).Returns(partidaExistente);

        var command = new SincronizarFixturesDaLigaCommand(Liga.Id, 2026);

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        partidaExistente.Situacao.Should().Be(SituacaoDaPartida.Encerrada);
        partidaExistente.GolsCasa.Should().Be(2);
        partidaExistente.GolsVisitante.Should().Be(1);
    }
}
