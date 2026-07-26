using MediatR;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Cotacoes.GetCotacaoMaisRecente;
using Oddify.Modules.Fixtures.Application.Cotacoes.GetCotacoesPorPartida;
using Oddify.Modules.Fixtures.Application.Ligas.GetLiga;
using Oddify.Modules.Fixtures.Application.Partidas.GetHistoricoRecentePorEquipe;
using Oddify.Modules.Fixtures.Application.Partidas.GetPartida;
using Contracts = Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Fixtures.Infrastructure.PublicApi;

internal sealed class FixturesApi(ISender sender) : Contracts.IFixturesApi
{
    public async Task<Result<Contracts.LigaResponse>> ObterLigaAsync(Guid ligaId, CancellationToken cancellationToken = default)
    {
        Result<LigaResponse> result = await sender.Send(new GetLigaQuery(ligaId), cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<Contracts.LigaResponse>(result.Error);
        }

        LigaResponse liga = result.Value;

        return new Contracts.LigaResponse(liga.Id, liga.Nome, liga.MediaDeGols, liga.FatorCasa, liga.Calibrada);
    }

    public async Task<Result<Contracts.HistoricoDeEquipeResponse>> ObterHistoricoRecenteAsync(
        Guid equipeId,
        int minimoDeJogos,
        CancellationToken cancellationToken = default)
    {
        Result<HistoricoDeEquipeResponse> result =
            await sender.Send(new GetHistoricoRecentePorEquipeQuery(equipeId, minimoDeJogos), cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<Contracts.HistoricoDeEquipeResponse>(result.Error);
        }

        HistoricoDeEquipeResponse historico = result.Value;

        return new Contracts.HistoricoDeEquipeResponse(historico.AmostraDeJogos, historico.MediaGolsFeitos, historico.MediaGolsSofridos);
    }

    public async Task<Result<Contracts.CotacaoResponse>> ObterCotacaoMaisRecenteAsync(
        Guid partidaId,
        string mercado,
        CancellationToken cancellationToken = default)
    {
        Result<CotacaoResponse> result = await sender.Send(new GetCotacaoMaisRecenteQuery(partidaId, mercado), cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<Contracts.CotacaoResponse>(result.Error);
        }

        CotacaoResponse cotacao = result.Value;

        return new Contracts.CotacaoResponse(
            cotacao.Id,
            cotacao.PartidaId,
            cotacao.Mercado,
            cotacao.Odd,
            cotacao.Casa,
            cotacao.ColetadaEmUtc);
    }

    public async Task<Result<Contracts.PartidaResponse>> ObterPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default)
    {
        Result<PartidaResponse> result = await sender.Send(new GetPartidaQuery(partidaId), cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<Contracts.PartidaResponse>(result.Error);
        }

        PartidaResponse partida = result.Value;

        return new Contracts.PartidaResponse(
            partida.Id,
            partida.LigaId,
            partida.EquipeCasaId,
            partida.EquipeVisitanteId,
            partida.DataUtc,
            partida.Situacao.ToString(),
            partida.GolsCasa,
            partida.GolsVisitante);
    }
}
