using Microsoft.Extensions.Options;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Abstractions.Data;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;
using Oddify.Modules.Fixtures.Domain.Cotacoes;
using Oddify.Modules.Fixtures.Domain.Equipes;
using Oddify.Modules.Fixtures.Domain.Ligas;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.Cotacoes.SincronizarCotacoes;

internal sealed class SincronizarCotacoesCommandHandler(
    IPartidaRepository partidaRepository,
    IEquipeRepository equipeRepository,
    ILigaConfiguradaRepository ligaRepository,
    ICotacaoRepository cotacaoRepository,
    ITheOddsApiClient theOddsApiClient,
    IOptions<SincronizacaoExternaOptions> opcoes,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SincronizarCotacoesCommand>
{
    private static readonly TimeSpan ToleranciaDeHorario = TimeSpan.FromHours(3);
    private static readonly TimeSpan JanelaDeBusca = TimeSpan.FromHours(48);

    public async Task<Result> Handle(SincronizarCotacoesCommand request, CancellationToken cancellationToken)
    {
        DateTime agora = DateTime.UtcNow;

        IReadOnlyCollection<Partida> partidasProximas =
            await partidaRepository.ListarAgendadasEntreAsync(agora, agora + JanelaDeBusca, cancellationToken);

        foreach (IGrouping<Guid, Partida> grupo in partidasProximas.GroupBy(p => p.LigaId))
        {
            await SincronizarLigaAsync(grupo.Key, [.. grupo], cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task SincronizarLigaAsync(Guid ligaId, IReadOnlyCollection<Partida> partidas, CancellationToken cancellationToken)
    {
        LigaConfigurada? liga = await ligaRepository.GetAsync(ligaId, cancellationToken);

        if (liga is null || !opcoes.Value.TheOddsApiSportKeys.TryGetValue(liga.IdExterno, out string? sportKey))
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
            await SincronizarPartidaAsync(partida, eventosResult.Value, cancellationToken);
        }
    }

    private async Task SincronizarPartidaAsync(Partida partida, IReadOnlyCollection<EventoDeOddsExternoDto> eventos, CancellationToken cancellationToken)
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
