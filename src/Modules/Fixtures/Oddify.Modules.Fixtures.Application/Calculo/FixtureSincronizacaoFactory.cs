using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.Calculo;

// Fetch-or-create de Equipe/Partida a partir de um fixture externo — "Factory" porque constrói (ou
// atualiza) os agregados que o Handler persiste depois via um único SaveChangesAsync no fim do loop.
// equipesSincronizadas é o cache desta execução: o mesmo time aparece em várias partidas (casa/fora)
// antes do primeiro SaveChanges, então uma nova consulta ao repositório não veria a inserção ainda
// não persistida e recriaria o time repetidas vezes.
public static class FixtureSincronizacaoFactory
{
    public static async Task SincronizarFixtureAsync(
        Guid ligaId,
        FixtureExternoDto fixture,
        int temporada,
        IEquipeRepository equipeRepository,
        IPartidaRepository partidaRepository,
        Dictionary<string, Equipe> equipesSincronizadas,
        CancellationToken cancellationToken)
    {
        Equipe equipeCasa = await ObterOuCriarEquipeAsync(fixture.EquipeCasaIdExterno, fixture.NomeEquipeCasa, fixture.EquipeCasaLogo, ligaId,
            equipeRepository, equipesSincronizadas, cancellationToken);
        Equipe equipeVisitante = await ObterOuCriarEquipeAsync(
            fixture.EquipeVisitanteIdExterno, fixture.NomeEquipeVisitante, fixture.EquipeVisitanteLogo, ligaId,
            equipeRepository, equipesSincronizadas, cancellationToken);

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

    private static async Task<Equipe> ObterOuCriarEquipeAsync(
        string idExterno,
        string nome,
        string? logo,
        Guid ligaId,
        IEquipeRepository equipeRepository,
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
