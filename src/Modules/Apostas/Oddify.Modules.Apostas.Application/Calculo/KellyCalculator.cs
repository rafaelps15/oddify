namespace Oddify.Modules.Apostas.Application.Calculo;

internal static class KellyCalculator
{
    public const decimal FracaoDeKelly = 0.25m;
    public const decimal TetoDeStakeSobreABanca = 0.05m;

    public static decimal CalcularStake(decimal saldoDaBanca, decimal probabilidade, decimal odd) =>
        CalcularStake(saldoDaBanca, probabilidade, odd, FracaoDeKelly);

    // Overload com fração explícita — só pra preview (GetSugestaoDeStakeQuery, a calculadora de
    // alavancagem do front), que deixa o usuário comparar Kelly cheio/meio/um-quarto antes de
    // apostar de verdade. MontarMultiplaCommandHandler (a montagem real da aposta) usa sempre o
    // overload de 3 argumentos acima, com a fração fixa — nunca aceita fração vinda do cliente
    // pra dinheiro de verdade.
    public static decimal CalcularStake(decimal saldoDaBanca, decimal probabilidade, decimal odd, decimal fracaoDeKelly)
    {
        decimal kelly = (probabilidade * odd - 1) / (odd - 1);

        if (kelly <= 0)
        {
            return 0m;
        }

        decimal stake = saldoDaBanca * kelly * fracaoDeKelly;
        decimal tetoDeStake = saldoDaBanca * TetoDeStakeSobreABanca;

        return Math.Min(stake, tetoDeStake);
    }
}
