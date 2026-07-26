namespace Oddify.Modules.Analise.Application.Calculo;

internal static class DixonColesCorrecao
{
    public const decimal RhoPadrao = -0.1m;

    // CA1814: uma matriz bidimensional é a representação natural de uma matriz de placares (gols da casa x gols do visitante).
#pragma warning disable CA1814
    public static decimal[,] Aplicar(decimal[,] matriz, decimal lambdaCasa, decimal lambdaVisitante, decimal rho = RhoPadrao)
    {
        decimal[,] corrigida = (decimal[,])matriz.Clone();

        corrigida[0, 0] = matriz[0, 0] * (1 - lambdaCasa * lambdaVisitante * rho);
        corrigida[1, 0] = matriz[1, 0] * (1 + lambdaVisitante * rho);
        corrigida[0, 1] = matriz[0, 1] * (1 + lambdaCasa * rho);
        corrigida[1, 1] = matriz[1, 1] * (1 - rho);

        decimal soma = 0m;

        foreach (decimal valor in corrigida)
        {
            soma += valor;
        }

        if (soma <= 0)
        {
            return corrigida;
        }

        for (int i = 0; i < corrigida.GetLength(0); i++)
        {
            for (int j = 0; j < corrigida.GetLength(1); j++)
            {
                corrigida[i, j] /= soma;
            }
        }

        return corrigida;
    }
#pragma warning restore CA1814
}
