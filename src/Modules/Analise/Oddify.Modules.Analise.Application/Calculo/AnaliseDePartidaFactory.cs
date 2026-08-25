using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.Modules.Analise.Application.Calculo;

// Fetch (I/O, múltiplos repositórios) + construção do resultado calculado — o "Factory" do padrão
// Kamil Grzybek (modular-monolith-with-ddd, ver PriceListFactory): static, sem interface, sem DI,
// recebe as dependências como parâmetros do método em vez de campos de instância. O Handler que
// consome isto (AnalisarPartidaCommandHandler) fica só com Handle, sem métodos privados — este
// Factory que carrega os dados e delega o cálculo puro para AnaliseDePartidaCalculator.
public static class AnaliseDePartidaFactory
{
    private const int JanelaDeJogosRecentes = 10;

    public static async Task<AnaliseCalculada?> ObterCalculoAsync(
        Guid partidaId,
        string mercado,
        ILigaRepository ligaRepository,
        IPartidaRepository partidaRepository,
        ICotacaoRepository cotacaoRepository,
        CancellationToken cancellationToken)
    {
        Partida? partida = await partidaRepository.GetAsync(partidaId, cancellationToken);
        if (partida is null)
        {
            return null;
        }

        Liga? liga = await ligaRepository.GetAsync(partida.LigaId, cancellationToken);
        if (liga is null)
        {
            return null;
        }

        HistoricoDeEquipe historicoCasa = HistoricoDeEquipeCalculator.Calcular(
            await partidaRepository.GetRecentesPorEquipeAsync(partida.EquipeCasaId, JanelaDeJogosRecentes, cancellationToken),
            partida.EquipeCasaId);

        HistoricoDeEquipe historicoVisitante = HistoricoDeEquipeCalculator.Calcular(
            await partidaRepository.GetRecentesPorEquipeAsync(partida.EquipeVisitanteId, JanelaDeJogosRecentes, cancellationToken),
            partida.EquipeVisitanteId);

        Cotacao? cotacao = await cotacaoRepository.GetMaisRecenteAsync(partidaId, mercado, cancellationToken);
        if (cotacao is null)
        {
            return null;
        }

        IReadOnlyDictionary<string, decimal> oddsDoGrupoDeMercado =
            await ObterOddsDoGrupoNaMesmaCasaAsync(partidaId, mercado, cotacao.Casa, cotacaoRepository, cancellationToken);

        if (!RemovedorDeMargem.GrupoCompleto(mercado, oddsDoGrupoDeMercado))
        {
            // Cobertura incompleta da casa para as demais saídas do mercado (ex.: só over sem under) —
            // não dá para remover a margem com segurança, então a partida fica sem análise desta vez.
            return null;
        }

        return AnaliseDePartidaCalculator.Calcular(liga, historicoCasa, historicoVisitante, cotacao, oddsDoGrupoDeMercado, mercado);
    }

    private static async Task<IReadOnlyDictionary<string, decimal>> ObterOddsDoGrupoNaMesmaCasaAsync(
        Guid partidaId,
        string mercado,
        string casa,
        ICotacaoRepository cotacaoRepository,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Cotacao> cotacoes = await cotacaoRepository.GetPorPartidaAsync(partidaId, cancellationToken);
        IReadOnlyCollection<string> grupo = MercadoResolver.ObterGrupoDeMercados(mercado);

        var oddsPorMercado = new Dictionary<string, decimal>();

        // cotacoes ja vem ordenada por ColetadaEmUtc desc (ICotacaoRepository.GetPorPartidaAsync) — a
        // primeira ocorrencia de cada mercado é a mais recente, por isso TryAdd (nunca sobrescreve) é
        // suficiente aqui.
        foreach (Cotacao cotacaoDoGrupo in cotacoes.Where(c => c.Casa == casa && grupo.Contains(c.Mercado)))
        {
            oddsPorMercado.TryAdd(cotacaoDoGrupo.Mercado, cotacaoDoGrupo.Odd);
        }

        return oddsPorMercado;
    }
}
