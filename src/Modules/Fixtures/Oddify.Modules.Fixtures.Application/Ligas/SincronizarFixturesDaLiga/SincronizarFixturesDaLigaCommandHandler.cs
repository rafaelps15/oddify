using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.Ligas.SincronizarFixturesDaLiga;

internal sealed class SincronizarFixturesDaLigaCommandHandler(
    ILigaConfiguradaRepository ligaRepository,
    IEquipeRepository equipeRepository,
    IPartidaRepository partidaRepository,
    IApiFootballClient apiFootballClient,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SincronizarFixturesDaLigaCommand>
{
    public async Task<Result> Handle(SincronizarFixturesDaLigaCommand request, CancellationToken cancellationToken)
    {
        LigaConfigurada? liga = await ligaRepository.GetAsync(request.LigaId, cancellationToken);

        if (liga is null)
        {
            return Result.Failure(LigaConfiguradaErrors.NotFound(request.LigaId));
        }

        Result<IReadOnlyCollection<FixtureExternoDto>> fixturesResult =
            await apiFootballClient.GetFixturesAsync(liga.IdExterno, request.Temporada, cancellationToken);

        if (fixturesResult.IsFailure)
        {
            return Result.Failure(fixturesResult.Error);
        }

        foreach (FixtureExternoDto fixture in fixturesResult.Value)
        {
            await SincronizarFixtureAsync(liga.Id, fixture, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task SincronizarFixtureAsync(Guid ligaId, FixtureExternoDto fixture, CancellationToken cancellationToken)
    {
        Equipe equipeCasa = await ObterOuCriarEquipeAsync(fixture.EquipeCasaIdExterno, fixture.NomeEquipeCasa, ligaId, cancellationToken);
        Equipe equipeVisitante = await ObterOuCriarEquipeAsync(fixture.EquipeVisitanteIdExterno, fixture.NomeEquipeVisitante, ligaId, cancellationToken);

        Partida? partida = await partidaRepository.GetByIdExternoAsync(fixture.IdExterno, cancellationToken);

        if (partida is null)
        {
            partida = Partida.Create(fixture.IdExterno, ligaId, equipeCasa.Id, equipeVisitante.Id, fixture.DataUtc);
            partidaRepository.Insert(partida);
        }

        if (fixture is { Encerrada: true, GolsCasa: not null, GolsVisitante: not null } && partida.Situacao == SituacaoDaPartida.Agendada)
        {
            partida.RegistrarResultado(fixture.GolsCasa.Value, fixture.GolsVisitante.Value);
        }
    }

    private async Task<Equipe> ObterOuCriarEquipeAsync(string idExterno, string nome, Guid ligaId, CancellationToken cancellationToken)
    {
        Equipe? equipe = await equipeRepository.GetByIdExternoAsync(idExterno, cancellationToken);

        if (equipe is not null)
        {
            return equipe;
        }

        equipe = Equipe.Create(idExterno, nome, ligaId);
        equipeRepository.Insert(equipe);

        return equipe;
    }
}
