using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Analise.Application.Calculo;

public static class AnaliseDePartidaCalculator
{
    public static AnaliseCalculada Calcular(
        LigaResponse liga,
        HistoricoDeEquipeResponse historicoCasa,
        HistoricoDeEquipeResponse historicoVisitante,
        CotacaoResponse cotacao,
        string mercado)
    {
        (decimal lambdaCasa, decimal lambdaVisitante) = PoissonCalculator.CalcularLambdas(
            liga.MediaDeGols,
            liga.FatorCasa,
            historicoCasa.MediaGolsFeitos,
            historicoCasa.MediaGolsSofridos,
            historicoVisitante.MediaGolsFeitos,
            historicoVisitante.MediaGolsSofridos);

        decimal[,] matrizPura = PoissonCalculator.MatrizDePlacares(lambdaCasa, lambdaVisitante);
        decimal probPoissonPura = ProbabilidadeDeMercadoCalculator.Calcular(matrizPura, mercado);

        decimal[,] matrizCorrigida = DixonColesCorrecao.Aplicar(matrizPura, lambdaCasa, lambdaVisitante);
        decimal probDixonColes = ProbabilidadeDeMercadoCalculator.Calcular(matrizCorrigida, mercado);

        decimal probImplicitaDaOdd = 1m / cotacao.Odd;
        decimal vantagem = probDixonColes - probImplicitaDaOdd;
        int amostraMinima = Math.Min(historicoCasa.AmostraDeJogos, historicoVisitante.AmostraDeJogos);

        (bool aprovada, string? motivo) = FiltroDeOportunidades.Avaliar(vantagem, cotacao.Odd, amostraMinima, liga.Calibrada);

        return new AnaliseCalculada(probPoissonPura, probDixonColes, probImplicitaDaOdd, vantagem, cotacao.Odd, aprovada, motivo);
    }
}

public sealed record AnaliseCalculada(
    decimal ProbPoissonPura,
    decimal ProbDixonColes,
    decimal ProbImplicitaDaOdd,
    decimal Vantagem,
    decimal Odd,
    bool Aprovada,
    string? Motivo);
