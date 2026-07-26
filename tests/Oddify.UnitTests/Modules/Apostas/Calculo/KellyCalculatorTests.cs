using FluentAssertions;
using Oddify.Modules.Apostas.Application.Calculo;

namespace Oddify.UnitTests.Modules.Apostas.Calculo;

public sealed class KellyCalculatorTests
{
    [Fact]
    public void CalcularStake_should_return_zero_when_there_is_no_edge()
    {
        // probabilidade * odd = 0.5 * 2.0 = 1.0 -> kelly = (1.0 - 1) / (odd - 1) = 0
        decimal stake = KellyCalculator.CalcularStake(saldoDaBanca: 1000m, probabilidade: 0.5m, odd: 2.0m);

        stake.Should().Be(0m);
    }

    [Fact]
    public void CalcularStake_should_return_zero_when_probabilidade_times_odd_is_below_one()
    {
        decimal stake = KellyCalculator.CalcularStake(saldoDaBanca: 1000m, probabilidade: 0.4m, odd: 2.0m);

        stake.Should().Be(0m);
    }

    [Fact]
    public void CalcularStake_should_return_positive_stake_when_there_is_edge()
    {
        // probabilidade 0.60, odd 2.0 -> kelly = (0.60*2.0 - 1) / (2.0 - 1) = 0.20
        // stake = saldo * kelly * 0.25 = 1000 * 0.20 * 0.25 = 50
        decimal stake = KellyCalculator.CalcularStake(saldoDaBanca: 1000m, probabilidade: 0.60m, odd: 2.0m);

        stake.Should().Be(50m);
    }

    [Fact]
    public void CalcularStake_should_scale_linearly_with_saldo_da_banca()
    {
        decimal stakeComMilBanca = KellyCalculator.CalcularStake(1000m, 0.60m, 2.0m);
        decimal stakeComDoisMilBanca = KellyCalculator.CalcularStake(2000m, 0.60m, 2.0m);

        stakeComDoisMilBanca.Should().Be(stakeComMilBanca * 2);
    }
}
