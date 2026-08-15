using MediatR;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Cotacoes.GetCotacaoMaisRecente;
using Oddify.Modules.Fixtures.Application.Cotacoes.GetCotacoesPorPartida;
using Oddify.Modules.Fixtures.Application.Ligas.GetLiga;
using Oddify.Modules.Fixtures.Application.Partidas.GetHistoricoRecentePorEquipe;
using Oddify.Modules.Fixtures.Application.Partidas.GetPartida;
using Oddify.Modules.Fixtures.Application.Partidas.GetPartidas;
using Oddify.Modules.Fixtures.Application.Partidas.GetPartidasResumo;
using Oddify.Modules.Fixtures.PublicApi;
using CotacaoResponse = Oddify.Modules.Fixtures.PublicApi.CotacaoResponse;
using HistoricoDeEquipeResponse = Oddify.Modules.Fixtures.PublicApi.HistoricoDeEquipeResponse;
using LigaResponse = Oddify.Modules.Fixtures.PublicApi.LigaResponse;
using PartidaResponse = Oddify.Modules.Fixtures.PublicApi.PartidaResponse;
using PartidaResumoResponse = Oddify.Modules.Fixtures.PublicApi.PartidaResumoResponse;

namespace Oddify.Modules.Fixtures.Infrastructure.PublicApi;

internal sealed class FixturesApi(ISender sender) : IFixturesApi
{
    public async Task<LigaResponse?> ObterLigaAsync(Guid ligaId, CancellationToken cancellationToken = default)
    {
        Result<Application.Ligas.GetLiga.LigaResponse> result = await sender.Send(new GetLigaQuery(ligaId), cancellationToken);

        if (result.IsFailure)
        {
            return null;
        }

        return new LigaResponse(result.Value.Id, result.Value.Nome, result.Value.MediaDeGols, result.Value.FatorCasa, result.Value.Calibrada);
    }

    public async Task<HistoricoDeEquipeResponse?> ObterHistoricoRecenteAsync(
        Guid equipeId,
        int minimoDeJogos,
        CancellationToken cancellationToken = default)
    {
        Result<Application.Partidas.GetHistoricoRecentePorEquipe.HistoricoDeEquipeResponse> result =
            await sender.Send(new GetHistoricoRecentePorEquipeQuery(equipeId, minimoDeJogos), cancellationToken);

        if (result.IsFailure)
        {
            return null;
        }

        return new HistoricoDeEquipeResponse(result.Value.AmostraDeJogos, result.Value.MediaGolsFeitos, result.Value.MediaGolsSofridos);
    }

    public async Task<CotacaoResponse?> ObterCotacaoMaisRecenteAsync(
        Guid partidaId,
        string mercado,
        CancellationToken cancellationToken = default)
    {
        Result<Application.Cotacoes.GetCotacoesPorPartida.CotacaoResponse> result =
            await sender.Send(new GetCotacaoMaisRecenteQuery(partidaId, mercado), cancellationToken);

        if (result.IsFailure)
        {
            return null;
        }

        return new CotacaoResponse(
            result.Value.Id,
            result.Value.PartidaId,
            result.Value.Mercado,
            result.Value.Odd,
            result.Value.Casa,
            result.Value.ColetadaEmUtc);
    }

    public async Task<IReadOnlyCollection<CotacaoResponse>> ObterCotacoesPorPartidaAsync(
        Guid partidaId,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyCollection<Application.Cotacoes.GetCotacoesPorPartida.CotacaoResponse>> result =
            await sender.Send(new GetCotacoesPorPartidaQuery(partidaId), cancellationToken);

        if (result.IsFailure)
        {
            return [];
        }

        return result.Value
            .Select(c => new CotacaoResponse(c.Id, c.PartidaId, c.Mercado, c.Odd, c.Casa, c.ColetadaEmUtc))
            .ToList();
    }

    public async Task<PartidaResponse?> ObterPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default)
    {
        Result<Application.Partidas.GetPartida.PartidaResponse> result = await sender.Send(new GetPartidaQuery(partidaId), cancellationToken);

        if (result.IsFailure)
        {
            return null;
        }

        return new PartidaResponse(
            result.Value.Id,
            result.Value.LigaId,
            result.Value.EquipeCasaId,
            result.Value.EquipeVisitanteId,
            result.Value.DataUtc,
            result.Value.Situacao.ToString(),
            result.Value.GolsCasa,
            result.Value.GolsVisitante);
    }

    public async Task<IReadOnlyCollection<PartidaResponse>> ObterPartidasAsync(
        IReadOnlyCollection<Guid> partidaIds,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyCollection<Application.Partidas.GetPartida.PartidaResponse>> result =
            await sender.Send(new GetPartidasQuery(LigaId: null, Ids: [.. partidaIds]), cancellationToken);

        if (result.IsFailure)
        {
            return [];
        }

        return result.Value
            .Select(p => new PartidaResponse(
                p.Id,
                p.LigaId,
                p.EquipeCasaId,
                p.EquipeVisitanteId,
                p.DataUtc,
                p.Situacao.ToString(),
                p.GolsCasa,
                p.GolsVisitante))
            .ToList();
    }

    public async Task<IReadOnlyCollection<PartidaResumoResponse>> ObterPartidasResumoAsync(
        IReadOnlyCollection<Guid> partidaIds,
        CancellationToken cancellationToken = default)
    {
        Result<IReadOnlyCollection<Application.Partidas.GetPartidasResumo.PartidaResumoResponse>> result =
            await sender.Send(new GetPartidasResumoQuery([.. partidaIds]), cancellationToken);

        if (result.IsFailure)
        {
            return [];
        }

        return result.Value
            .Select(p => new PartidaResumoResponse(
                p.Id,
                p.LigaId,
                p.LigaNome,
                p.DataUtc,
                p.EquipeCasaId,
                p.EquipeCasaNome,
                p.EquipeCasaLogo,
                p.EquipeVisitanteId,
                p.EquipeVisitanteNome,
                p.EquipeVisitanteLogo))
            .ToList();
    }
}
