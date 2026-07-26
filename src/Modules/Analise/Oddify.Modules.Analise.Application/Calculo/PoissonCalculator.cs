namespace Oddify.Modules.Analise.Application.Calculo;

internal static class PoissonCalculator
{
    public const int MaxGolsNaMatriz = 9;

    public static (decimal LambdaCasa, decimal LambdaVisitante) CalcularLambdas(
        decimal mediaDeGolsDaLiga,
        decimal fatorCasa,
        decimal mediaGolsFeitosCasa,
        decimal mediaGolsSofridosCasa,
        decimal mediaGolsFeitosVisitante,
        decimal mediaGolsSofridosVisitante)
    {
        decimal lambdaCasa = mediaDeGolsDaLiga
            * (mediaGolsFeitosCasa / mediaDeGolsDaLiga)
            * (mediaGolsSofridosVisitante / mediaDeGolsDaLiga)
            * fatorCasa;

        decimal lambdaVisitante = mediaDeGolsDaLiga
            * (mediaGolsFeitosVisitante / mediaDeGolsDaLiga)
            * (mediaGolsSofridosCasa / mediaDeGolsDaLiga);

        return (lambdaCasa, lambdaVisitante);
    }

    // CA1814: uma matriz bidimensional é a representação natural de uma matriz de placares (gols da casa x gols do visitante).
#pragma warning disable CA1814
    public static decimal[,] MatrizDePlacares(decimal lambdaCasa, decimal lambdaVisitante, int maxGols = MaxGolsNaMatriz)
    {
        decimal[] probCasa = new decimal[maxGols + 1];
        decimal[] probVisitante = new decimal[maxGols + 1];

        for (int k = 0; k <= maxGols; k++)
        {
            probCasa[k] = ProbabilidadePoisson(lambdaCasa, k);
            probVisitante[k] = ProbabilidadePoisson(lambdaVisitante, k);
        }

        decimal[,] matriz = new decimal[maxGols + 1, maxGols + 1];

        for (int i = 0; i <= maxGols; i++)
        {
            for (int j = 0; j <= maxGols; j++)
            {
                matriz[i, j] = probCasa[i] * probVisitante[j];
            }
        }

        return matriz;
    }
#pragma warning restore CA1814

    public static decimal ProbabilidadePoisson(decimal lambda, int k)
    {
        double lambdaDouble = (double)lambda;
        double resultado = Math.Exp(-lambdaDouble) * Math.Pow(lambdaDouble, k) / Fatorial(k);
        return (decimal)resultado;
    }

    private static double Fatorial(int n) => n <= 1 ? 1 : n * Fatorial(n - 1);
}
