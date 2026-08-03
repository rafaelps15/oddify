using Oddify.Modules.Analise.Application.Calculo;
using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Analise.Application.Abstractions.Fixtures;

internal sealed class AnaliseDePartidaDadosService(IFixturesApi fixturesApi) : IAnaliseDePartidaDadosService
{
    private const int JanelaDeJogosRecentes = 10;

    public async Task<AnaliseCalculada?> ObterAsync(Guid partidaId, string mercado, CancellationToken cancellationToken = default)
    {
        PartidaResponse? partida = await fixturesApi.ObterPartidaAsync(partidaId, cancellationToken);
        if (partida is null)
        {
            return null;
        }

        LigaResponse? liga = await fixturesApi.ObterLigaAsync(partida.LigaId, cancellationToken);
        HistoricoDeEquipeResponse? historicoCasa = await fixturesApi.ObterHistoricoRecenteAsync(partida.EquipeCasaId, JanelaDeJogosRecentes, cancellationToken);
        HistoricoDeEquipeResponse? historicoVisitante = await fixturesApi.ObterHistoricoRecenteAsync(partida.EquipeVisitanteId, JanelaDeJogosRecentes, cancellationToken);
        CotacaoResponse? cotacao = await fixturesApi.ObterCotacaoMaisRecenteAsync(partidaId, mercado, cancellationToken);

        if (liga is null || historicoCasa is null || historicoVisitante is null || cotacao is null)
        {
            return null;
        }

        IReadOnlyDictionary<string, decimal> oddsDoGrupoDeMercado =
            await ObterOddsDoGrupoNaMesmaCasaAsync(partidaId, mercado, cotacao.Casa, cancellationToken);

        if (!RemovedorDeMargem.GrupoCompleto(mercado, oddsDoGrupoDeMercado))
        {
            // Cobertura incompleta da casa para as demais saídas do mercado (ex.: só over sem under) —
            // não dá para remover a margem com segurança, então a partida fica sem análise desta vez.
            return null;
        }

        return AnaliseDePartidaCalculator.Calcular(liga, historicoCasa, historicoVisitante, cotacao, oddsDoGrupoDeMercado, mercado);
    }

    private async Task<IReadOnlyDictionary<string, decimal>> ObterOddsDoGrupoNaMesmaCasaAsync(
        Guid partidaId,
        string mercado,
        string casa,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<CotacaoResponse> cotacoes = await fixturesApi.ObterCotacoesPorPartidaAsync(partidaId, cancellationToken);
        IReadOnlyCollection<string> grupo = MercadoResolver.ObterGrupoDeMercados(mercado);

        var oddsPorMercado = new Dictionary<string, decimal>();

        // cotacoes ja vem ordenada por coletada_em_utc desc (GetCotacoesPorPartidaQueryHandler) — a primeira
        // ocorrencia de cada mercado é a mais recente, por isso TryAdd (nunca sobrescreve) é suficiente aqui.
        foreach (CotacaoResponse cotacaoDoGrupo in cotacoes.Where(c => c.Casa == casa && grupo.Contains(c.Mercado)))
        {
            oddsPorMercado.TryAdd(cotacaoDoGrupo.Mercado, cotacaoDoGrupo.Odd);
        }

        return oddsPorMercado;
    }
}
