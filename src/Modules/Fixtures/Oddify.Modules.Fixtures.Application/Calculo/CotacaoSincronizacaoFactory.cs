using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;
using Oddify.Modules.Fixtures.Domain.Cotacoes;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.Calculo;

// Fetch de odds externas + Insert das Cotacoes que baterem com a partida/janela de horário — "Factory"
// porque monta os agregados (Cotacao) a partir de dados vindos de mais de uma fonte (o repositório de
// Ligas/Equipes e o cliente externo de odds) antes do Handler persistir tudo num único SaveChangesAsync.
public static class CotacaoSincronizacaoFactory
{
    private static readonly TimeSpan ToleranciaDeHorario = TimeSpan.FromHours(3);

    public static async Task SincronizarLigaAsync(
        Guid ligaId,
        IReadOnlyCollection<Partida> partidas,
        ILigaConfiguradaRepository ligaRepository,
        IEquipeRepository equipeRepository,
        ICotacaoRepository cotacaoRepository,
        ITheOddsApiClient theOddsApiClient,
        IReadOnlyDictionary<string, string> sportKeysPorLigaExterna,
        CancellationToken cancellationToken)
    {
        LigaConfigurada? liga = await ligaRepository.GetAsync(ligaId, cancellationToken);

        if (liga is null || !sportKeysPorLigaExterna.TryGetValue(liga.IdExterno, out string? sportKey))
        {
            return;
        }

        Result<IReadOnlyCollection<EventoDeOddsExternoDto>> eventosResult =
            await theOddsApiClient.GetOddsAsync(sportKey, cancellationToken);

        if (eventosResult.IsFailure)
        {
            return;
        }

        foreach (Partida partida in partidas)
        {
            await SincronizarPartidaAsync(partida, eventosResult.Value, equipeRepository, cotacaoRepository, cancellationToken);
        }
    }

    private static async Task SincronizarPartidaAsync(
        Partida partida,
        IReadOnlyCollection<EventoDeOddsExternoDto> eventos,
        IEquipeRepository equipeRepository,
        ICotacaoRepository cotacaoRepository,
        CancellationToken cancellationToken)
    {
        Equipe? equipeCasa = await equipeRepository.GetAsync(partida.EquipeCasaId, cancellationToken);
        Equipe? equipeVisitante = await equipeRepository.GetAsync(partida.EquipeVisitanteId, cancellationToken);

        if (equipeCasa is null || equipeVisitante is null)
        {
            return;
        }

        EventoDeOddsExternoDto? evento = eventos.FirstOrDefault(e =>
            string.Equals(e.NomeEquipeCasa, equipeCasa.Nome, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(e.NomeEquipeVisitante, equipeVisitante.Nome, StringComparison.OrdinalIgnoreCase) &&
            (e.CommenceTimeUtc - partida.DataUtc).Duration() <= ToleranciaDeHorario);

        if (evento is null)
        {
            return;
        }

        foreach (OutcomeDeOddsDto outcome in evento.Outcomes)
        {
            Result<Cotacao> resultado = Cotacao.Create(partida.Id, outcome.Mercado, outcome.Odd, outcome.Casa, DateTime.UtcNow);

            if (resultado.IsSuccess)
            {
                cotacaoRepository.Insert(resultado.Value);
            }
        }
    }
}
