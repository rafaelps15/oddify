using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Partidas.CriarPartida;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.UnitTests.Modules.Fixtures.Partidas;

public sealed class CriarPartidaCommandHandlerTests
{
    private readonly IPartidaRepository _partidaRepository = Substitute.For<IPartidaRepository>();
    private readonly ILigaConfiguradaRepository _ligaRepository = Substitute.For<ILigaConfiguradaRepository>();
    private readonly IEquipeRepository _equipeRepository = Substitute.For<IEquipeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static readonly LigaConfigurada Liga = LigaConfigurada.Create("liga-1", "Premier League", 2.5m, 1.1m);
    private static readonly Equipe EquipeCasa = Equipe.Create("time-casa", "Flamengo", Liga.Id);
    private static readonly Equipe EquipeVisitante = Equipe.Create("time-visitante", "Palmeiras", Liga.Id);

    private CriarPartidaCommandHandler CriarHandler() =>
        new(_partidaRepository, _ligaRepository, _equipeRepository, _unitOfWork);

    private static CriarPartidaCommand CriarComando(Guid? equipeVisitanteId = null) => new(
        "partida-1",
        Liga.Id,
        EquipeCasa.Id,
        equipeVisitanteId ?? EquipeVisitante.Id,
        DateTime.UtcNow.AddDays(1),
        Rodada: 4,
        Temporada: 2026);

    [Fact]
    public async Task Handle_should_create_partida_with_rodada_and_temporada_and_persist()
    {
        _ligaRepository.GetAsync(Liga.Id, Arg.Any<CancellationToken>()).Returns(Liga);
        _equipeRepository.GetAsync(EquipeCasa.Id, Arg.Any<CancellationToken>()).Returns(EquipeCasa);
        _equipeRepository.GetAsync(EquipeVisitante.Id, Arg.Any<CancellationToken>()).Returns(EquipeVisitante);

        Result<Guid> resultado = await CriarHandler().Handle(CriarComando(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _partidaRepository.Received(1).Insert(Arg.Is<Partida>(p => p.Rodada == 4 && p.Temporada == 2026));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_equipe_casa_and_visitante_are_the_same()
    {
        Result<Guid> resultado = await CriarHandler().Handle(CriarComando(equipeVisitanteId: EquipeCasa.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(PartidaErrors.EquipesIguais);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_liga_not_found()
    {
        _ligaRepository.GetAsync(Liga.Id, Arg.Any<CancellationToken>()).Returns((LigaConfigurada?)null);

        Result<Guid> resultado = await CriarHandler().Handle(CriarComando(), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(LigaConfiguradaErrors.NotFound(Liga.Id));
    }

    [Fact]
    public async Task Handle_should_fail_when_equipe_casa_not_found()
    {
        _ligaRepository.GetAsync(Liga.Id, Arg.Any<CancellationToken>()).Returns(Liga);
        _equipeRepository.GetAsync(EquipeCasa.Id, Arg.Any<CancellationToken>()).Returns((Equipe?)null);

        Result<Guid> resultado = await CriarHandler().Handle(CriarComando(), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(EquipeErrors.NotFound(EquipeCasa.Id));
    }
}
