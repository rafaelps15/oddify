using Microsoft.EntityFrameworkCore;
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

        string? bandeira = fixturesResult.Value.Select(fixture => fixture.LigaFlag).FirstOrDefault(flag => flag is not null);
        liga.AtualizarBandeira(bandeira);

        // Cache local por execução: o time aparece em várias partidas (casa/fora) antes do
        // SaveChangesAsync ser chamado, então a consulta ao repositório não veria as inserções
        // ainda não persistidas e recriaria o mesmo time repetidas vezes.
        var equipesSincronizadas = new Dictionary<string, Equipe>();

        foreach (FixtureExternoDto fixture in fixturesResult.Value)
        {
            await SincronizarFixtureAsync(liga.Id, fixture, request.Temporada, equipesSincronizadas, cancellationToken);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Corrida entre duas execuções concorrentes desta sincronização tentando inserir a
            // mesma Partida/Equipe (mesmo IdExterno) — ver UNIQUE INDEX em PartidaConfiguration/
            // EquipeConfiguration. Tratado como idempotente: a execução concorrente que venceu já
            // persistiu os dados corretos; a próxima sincronização periódica reconcilia sozinha.
            return Result.Failure(LigaConfiguradaErrors.SincronizacaoConcorrente(request.LigaId));
        }

        return Result.Success();
    }

    private async Task SincronizarFixtureAsync(
        Guid ligaId,
        FixtureExternoDto fixture,
        int temporada,
        Dictionary<string, Equipe> equipesSincronizadas,
        CancellationToken cancellationToken)
    {
        Equipe equipeCasa = await ObterOuCriarEquipeAsync(
            fixture.EquipeCasaIdExterno, fixture.NomeEquipeCasa, fixture.EquipeCasaLogo, ligaId, equipesSincronizadas, cancellationToken);
        Equipe equipeVisitante = await ObterOuCriarEquipeAsync(
            fixture.EquipeVisitanteIdExterno, fixture.NomeEquipeVisitante, fixture.EquipeVisitanteLogo, ligaId, equipesSincronizadas, cancellationToken);

        Partida? partida = await partidaRepository.GetByIdExternoAsync(fixture.IdExterno, cancellationToken);

        if (partida is null)
        {
            partida = Partida.Create(fixture.IdExterno, ligaId, equipeCasa.Id, equipeVisitante.Id, fixture.DataUtc, fixture.Rodada, temporada);
            partidaRepository.Insert(partida);
        }

        if (fixture is { Encerrada: true, GolsCasa: not null, GolsVisitante: not null } &&
            partida.Situacao is SituacaoDaPartida.Agendada or SituacaoDaPartida.EmAndamento)
        {
            partida.RegistrarResultado(fixture.GolsCasa.Value, fixture.GolsVisitante.Value);
        }
    }

    private async Task<Equipe> ObterOuCriarEquipeAsync(
        string idExterno,
        string nome,
        string? logo,
        Guid ligaId,
        Dictionary<string, Equipe> equipesSincronizadas,
        CancellationToken cancellationToken)
    {
        if (equipesSincronizadas.TryGetValue(idExterno, out Equipe? equipeEmMemoria))
        {
            return equipeEmMemoria;
        }

        Equipe? equipe = await equipeRepository.GetByIdExternoAsync(idExterno, ligaId, cancellationToken);

        if (equipe is null)
        {
            equipe = Equipe.Create(idExterno, nome, ligaId, logo);
            equipeRepository.Insert(equipe);
        }
        else
        {
            equipe.AtualizarLogo(logo);
        }

        equipesSincronizadas[idExterno] = equipe;

        return equipe;
    }
}
