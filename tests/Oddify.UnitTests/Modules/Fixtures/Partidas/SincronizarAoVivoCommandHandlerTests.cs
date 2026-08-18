using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;
using Oddify.Modules.Fixtures.Application.Partidas.SincronizarAoVivo;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.UnitTests.Modules.Fixtures.Partidas;

public sealed class SincronizarAoVivoCommandHandlerTests
{
    private readonly ILigaConfiguradaRepository _ligaRepository = Substitute.For<ILigaConfiguradaRepository>();
    private readonly IPartidaRepository _partidaRepository = Substitute.For<IPartidaRepository>();
    private readonly IApiFootballClient _apiFootballClient = Substitute.For<IApiFootballClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SincronizarAoVivoCommandHandler CriarHandler() =>
        new(_ligaRepository, _partidaRepository, _apiFootballClient, _unitOfWork);

    private static LigaConfigurada CriarLiga(string idExterno) => LigaConfigurada.Create(idExterno, "Liga", 2.5m, 1.3m, bandeira: null);

    [Fact]
    public async Task Handle_should_mark_partida_em_andamento_when_fixture_is_live()
    {
        LigaConfigurada liga = CriarLiga("39");
        _ligaRepository.ListarTodasAsync(Arg.Any<CancellationToken>()).Returns([liga]);

        var partida = Partida.Create("fixture-1", liga.Id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, rodada: 1, temporada: 2026);
        _partidaRepository.GetByIdExternoAsync("fixture-1", Arg.Any<CancellationToken>()).Returns(partida);

        _apiFootballClient.GetFixturesAoVivoAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<FixtureAoVivoExternoDto>>(
                [new FixtureAoVivoExternoDto("fixture-1", EmAndamento: true, Encerrada: false, GolsCasa: 1, GolsVisitante: 0)]));

        Result resultado = await CriarHandler().Handle(new SincronizarAoVivoCommand(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        partida.Situacao.Should().Be(SituacaoDaPartida.EmAndamento);
        partida.GolsCasa.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_registrar_resultado_when_fixture_just_finished()
    {
        LigaConfigurada liga = CriarLiga("39");
        _ligaRepository.ListarTodasAsync(Arg.Any<CancellationToken>()).Returns([liga]);

        var partida = Partida.Create("fixture-1", liga.Id, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, rodada: 1, temporada: 2026);
        partida.AtualizarAoVivo(1, 0);
        _partidaRepository.GetByIdExternoAsync("fixture-1", Arg.Any<CancellationToken>()).Returns(partida);

        _apiFootballClient.GetFixturesAoVivoAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<FixtureAoVivoExternoDto>>(
                [new FixtureAoVivoExternoDto("fixture-1", EmAndamento: false, Encerrada: true, GolsCasa: 2, GolsVisitante: 0)]));

        Result resultado = await CriarHandler().Handle(new SincronizarAoVivoCommand(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        partida.Situacao.Should().Be(SituacaoDaPartida.Encerrada);
        partida.GolsCasa.Should().Be(2);
    }

    [Fact]
    public async Task Handle_should_ignore_fixture_with_no_matching_partida()
    {
        LigaConfigurada liga = CriarLiga("39");
        _ligaRepository.ListarTodasAsync(Arg.Any<CancellationToken>()).Returns([liga]);
        _partidaRepository.GetByIdExternoAsync("fixture-desconhecida", Arg.Any<CancellationToken>()).Returns((Partida?)null);

        _apiFootballClient.GetFixturesAoVivoAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyCollection<FixtureAoVivoExternoDto>>(
                [new FixtureAoVivoExternoDto("fixture-desconhecida", EmAndamento: true, Encerrada: false, GolsCasa: 0, GolsVisitante: 0)]));

        Result resultado = await CriarHandler().Handle(new SincronizarAoVivoCommand(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_return_failure_when_api_football_client_fails()
    {
        _ligaRepository.ListarTodasAsync(Arg.Any<CancellationToken>()).Returns([]);
        var erro = Error.Failure("Fixtures.ApiFootballIndisponivel", "indisponível");
        _apiFootballClient.GetFixturesAoVivoAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyCollection<FixtureAoVivoExternoDto>>(erro));

        Result resultado = await CriarHandler().Handle(new SincronizarAoVivoCommand(), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(erro);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
