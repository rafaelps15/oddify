using Oddify.Modules.Analise.Application.Calculo;
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

        return AnaliseDePartidaCalculator.Calcular(liga, historicoCasa, historicoVisitante, cotacao, mercado);
    }
}
