namespace Oddify.Modules.Apostas.Application.Calculo;

internal static class KellyCalculator
{
    public const decimal FracaoDeKelly = 0.25m;

    public static decimal CalcularStake(decimal saldoDaBanca, decimal probabilidade, decimal odd)
    {
        decimal kelly = (probabilidade * odd - 1) / (odd - 1);

        if (kelly <= 0)
        {
            return 0m;
        }

        return saldoDaBanca * kelly * FracaoDeKelly;
    }
}
