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

        foreach (FixtureAoVivoExternoDto fixture in fixturesResult.Value)
        {
            await AtualizarFixtureAsync(fixture, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    // Best-effort por fixture — uma partida não encontrada (ainda não sincronizada pela temporada)
    // ou uma transição inválida (ex.: já Liquidada) não derruba o ciclo inteiro, mesmo padrão de
    // SincronizarFixturesDaLigaCommandHandler.SincronizarFixtureAsync.
    private async Task AtualizarFixtureAsync(FixtureAoVivoExternoDto fixture, CancellationToken cancellationToken)
    {
        if (fixture.GolsCasa is null || fixture.GolsVisitante is null)
        {
            return;
        }

        Partida? partida = await partidaRepository.GetByIdExternoAsync(fixture.IdExterno, cancellationToken);

        if (partida is null)
        {
            return;
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
}
