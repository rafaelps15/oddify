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

    [Fact]
    public void CalcularStake_should_cap_at_five_percent_of_banca_when_kelly_fraction_exceeds_it()
    {
        // probabilidade 0.95, odd 3.0 -> kelly = (0.95*3.0 - 1) / (3.0 - 1) = 0.925
        // stake sem teto seria 1000 * 0.925 * 0.25 = 231,25 — bem acima do teto de 5% (50)
        decimal stake = KellyCalculator.CalcularStake(saldoDaBanca: 1000m, probabilidade: 0.95m, odd: 3.0m);

        stake.Should().Be(1000m * KellyCalculator.TetoDeStakeSobreABanca);
    }

    [Fact]
    public void CalcularStake_should_not_cap_when_kelly_fraction_stake_is_within_the_limit()
    {
        decimal stake = KellyCalculator.CalcularStake(saldoDaBanca: 1000m, probabilidade: 0.55m, odd: 2.0m);

        stake.Should().BeLessThan(1000m * KellyCalculator.TetoDeStakeSobreABanca);
    }
}
