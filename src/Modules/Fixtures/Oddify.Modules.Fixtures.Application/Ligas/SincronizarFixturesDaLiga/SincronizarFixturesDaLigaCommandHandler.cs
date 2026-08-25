using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;
using Oddify.Modules.Fixtures.Application.Calculo;
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

        string? bandeira = fixturesResult.Value.Select(fixture => fixture.LigaFlag).FirstOrDefault(flag => flag is not null);
        liga.AtualizarBandeira(bandeira);

        // Cache local por execução: o time aparece em várias partidas (casa/fora) antes do
        // SaveChangesAsync ser chamado, então a consulta ao repositório não veria as inserções
        // ainda não persistidas e recriaria o mesmo time repetidas vezes.
        var equipesSincronizadas = new Dictionary<string, Equipe>();

        foreach (FixtureExternoDto fixture in fixturesResult.Value)
        {
            await FixtureSincronizacaoFactory.SincronizarFixtureAsync(
                liga.Id, fixture, request.Temporada, equipeRepository, partidaRepository, equipesSincronizadas, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
