using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.Modules.Analise.Application.Calculo;

internal static class ProbabilidadeDeMercadoCalculator
{
    // CA1814: uma matriz bidimensional é a representação natural de uma matriz de placares (gols da casa x gols do visitante).
#pragma warning disable CA1814
    public static decimal Calcular(decimal[,] matriz, string mercado)
    {
        decimal soma = 0m;

        for (int i = 0; i < matriz.GetLength(0); i++)
        {
            for (int j = 0; j < matriz.GetLength(1); j++)
            {
                if (MercadoResolver.Resolver(mercado, i, j))
                {
                    soma += matriz[i, j];
                }
            }
        }

        return soma;
    }
#pragma warning restore CA1814
}
