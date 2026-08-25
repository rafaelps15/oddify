using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.Partidas.SincronizarAoVivo;

internal sealed class SincronizarAoVivoCommandHandler(
    ILigaConfiguradaRepository ligaRepository,
    IPartidaRepository partidaRepository,
    IApiFootballClient apiFootballClient,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SincronizarAoVivoCommand>
{
    public async Task<Result> Handle(SincronizarAoVivoCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<LigaConfigurada> ligas = await ligaRepository.ListarTodasAsync(cancellationToken);
        string[] ligaIdsExternos = [.. ligas.Select(liga => liga.IdExterno)];

        Result<IReadOnlyCollection<FixtureAoVivoExternoDto>> fixturesResult =
            await apiFootballClient.GetFixturesAoVivoAsync(ligaIdsExternos, cancellationToken);

        if (fixturesResult.IsFailure)
        {
            return Result.Failure(fixturesResult.Error);
        }

        // Best-effort por fixture — uma partida não encontrada (ainda não sincronizada pela temporada)
        // ou faltando gols não derruba o ciclo inteiro, por isso "continue" em vez de propagar falha.
        foreach (FixtureAoVivoExternoDto fixture in fixturesResult.Value)
        {
            if (fixture.GolsCasa is null || fixture.GolsVisitante is null)
            {
                continue;
            }

            Partida? partida = await partidaRepository.GetByIdExternoAsync(fixture.IdExterno, cancellationToken);

            if (partida is null)
            {
                continue;
            }

            if (fixture.EmAndamento)
            {
                partida.AtualizarAoVivo(fixture.GolsCasa.Value, fixture.GolsVisitante.Value);
            }
            else if (fixture.Encerrada)
            {
                partida.RegistrarResultado(fixture.GolsCasa.Value, fixture.GolsVisitante.Value);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
