using FluentAssertions;
using Oddify.Modules.Analise.Application.Calculo;

namespace Oddify.UnitTests.Modules.Analise.Calculo;

public sealed class RemovedorDeMargemTests
{
    [Fact]
    public void Remover_should_normalize_implicit_probabilities_to_sum_to_one_across_the_group()
    {
        var odds = new Dictionary<string, decimal>
        {
            ["vitoria_casa"] = 1.5m,
            ["empate"] = 4.2m,
            ["vitoria_visitante"] = 6.0m
        };

        decimal implicitaCasa = RemovedorDeMargem.Remover("vitoria_casa", odds);
        decimal implicitaEmpate = RemovedorDeMargem.Remover("empate", odds);
        decimal implicitaVisitante = RemovedorDeMargem.Remover("vitoria_visitante", odds);

        (implicitaCasa + implicitaEmpate + implicitaVisitante).Should().BeApproximately(1m, 0.0001m);
    }

    [Fact]
    public void Remover_should_return_lower_probability_than_raw_inverse_of_odd_when_market_has_overround()
    {
        var odds = new Dictionary<string, decimal>
        {
            ["vitoria_casa"] = 1.5m,
            ["empate"] = 4.2m,
            ["vitoria_visitante"] = 6.0m
        };

        decimal implicitaJusta = RemovedorDeMargem.Remover("vitoria_casa", odds);

        implicitaJusta.Should().BeLessThan(1m / 1.5m);
    }

    [Fact]
    public void Remover_should_normalize_totals_market_pair()
    {
        // over 2.5 a 1.90 e under 2.5 a 1.90 -> margem simetrica, ambos devem normalizar para 0.5
        var odds = new Dictionary<string, decimal>
        {
            ["over_2_5"] = 1.90m,
            ["under_2_5"] = 1.90m
        };

        RemovedorDeMargem.Remover("over_2_5", odds).Should().BeApproximately(0.5m, 0.0001m);
        RemovedorDeMargem.Remover("under_2_5", odds).Should().BeApproximately(0.5m, 0.0001m);
    }

    [Fact]
    public void Remover_should_throw_when_a_sibling_market_odd_is_missing()
    {
        var odds = new Dictionary<string, decimal> { ["over_2_5"] = 1.90m };

        Action act = () => RemovedorDeMargem.Remover("over_2_5", odds);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GrupoCompleto_should_return_false_when_a_sibling_market_odd_is_missing()
    {
        var odds = new Dictionary<string, decimal> { ["vitoria_casa"] = 1.5m, ["empate"] = 4.2m };

        RemovedorDeMargem.GrupoCompleto("vitoria_casa", odds).Should().BeFalse();
    }

    [Fact]
    public void GrupoCompleto_should_return_true_when_all_sibling_market_odds_are_present()
    {
        var odds = new Dictionary<string, decimal> { ["over_1_5"] = 1.30m, ["under_1_5"] = 3.20m };

        RemovedorDeMargem.GrupoCompleto("over_1_5", odds).Should().BeTrue();
    }
}
