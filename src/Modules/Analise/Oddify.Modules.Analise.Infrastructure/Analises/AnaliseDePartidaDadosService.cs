using Oddify.Common.Domain;
using Oddify.Modules.Analise.Application.Abstractions.Analises;
using Oddify.Modules.Analise.Application.Calculo;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Analise.Infrastructure.Analises;

internal sealed class AnaliseDePartidaDadosService(IFixturesApi fixturesApi) : IAnaliseDePartidaDadosService
{
    private const int JanelaDeJogosRecentes = 10;

    public async Task<AnaliseCalculada?> ObterAsync(Guid partidaId, string mercado, CancellationToken cancellationToken)
    {
        Result<PartidaResponse> partida = await fixturesApi.ObterPartidaAsync(partidaId, cancellationToken);
        if (partida.IsFailure)
        {
            return null;
        }

        Result<LigaResponse> liga = await fixturesApi.ObterLigaAsync(partida.Value.LigaId, cancellationToken);
        if (liga.IsFailure)
        {
            return null;
        }

        Result<HistoricoDeEquipeResponse> historicoCasa =
            await fixturesApi.ObterHistoricoRecenteAsync(partida.Value.EquipeCasaId, JanelaDeJogosRecentes, cancellationToken);
        if (historicoCasa.IsFailure)
        {
            return null;
        }

        Result<HistoricoDeEquipeResponse> historicoVisitante =
            await fixturesApi.ObterHistoricoRecenteAsync(partida.Value.EquipeVisitanteId, JanelaDeJogosRecentes, cancellationToken);
        if (historicoVisitante.IsFailure)
        {
            return null;
        }

        Result<CotacaoResponse> cotacao = await fixturesApi.ObterCotacaoMaisRecenteAsync(partidaId, mercado, cancellationToken);
        if (cotacao.IsFailure)
        {
            return null;
        }

        return AnaliseDePartidaCalculator.Calcular(liga.Value, historicoCasa.Value, historicoVisitante.Value, cotacao.Value, mercado);
    }
}
