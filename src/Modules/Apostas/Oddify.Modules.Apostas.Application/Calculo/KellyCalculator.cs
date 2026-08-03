namespace Oddify.Modules.Apostas.Application.Calculo;

internal static class KellyCalculator
{
    public const decimal FracaoDeKelly = 0.25m;
    public const decimal TetoDeStakeSobreABanca = 0.05m;

    public static decimal CalcularStake(decimal saldoDaBanca, decimal probabilidade, decimal odd)
    {
        decimal kelly = (probabilidade * odd - 1) / (odd - 1);

        if (kelly <= 0)
        {
            return 0m;
        }

        decimal stake = saldoDaBanca * kelly * FracaoDeKelly;
        decimal tetoDeStake = saldoDaBanca * TetoDeStakeSobreABanca;

        return Math.Min(stake, tetoDeStake);
    }
}
