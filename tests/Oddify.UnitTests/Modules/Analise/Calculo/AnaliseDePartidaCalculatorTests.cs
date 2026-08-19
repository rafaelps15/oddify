using FluentAssertions;
using Oddify.Modules.Analise.Application.Calculo;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.UnitTests.Modules.Analise.Calculo;

public sealed class AnaliseDePartidaCalculatorTests
{
    private static readonly Liga LigaDeTeste = Liga.Create(Guid.NewGuid(), "Liga de Teste", 2.5m, 1.1m, calibrada: true);

    // Time da casa ataca bem acima da média e o visitante defende mal (e vice-versa) para dar uma
    // margem folgada de vantagem, mesma configuração usada em AnalisarPartidaCommandHandlerTests.
    private static readonly HistoricoDeEquipe HistoricoCasa = new(AmostraDeJogos: 10, MediaGolsFeitos: 3.0m, MediaGolsSofridos: 1.0m);
    private static readonly HistoricoDeEquipe HistoricoVisitante = new(AmostraDeJogos: 10, MediaGolsFeitos: 1.0m, MediaGolsSofridos: 3.0m);

    private static Cotacao CriarCotacao(decimal odd) =>
        Cotacao.Create(Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", odd, "casa-de-apostas", DateTime.UtcNow);

    // Mercado 1X2 com overround de ~7,1% (1/1.5 + 1/4.2 + 1/6.0 = 1,0714) — típico de casa de apostas real.
    private static readonly Dictionary<string, decimal> Odds1X2ComMargem = new()
    {
        ["vitoria_casa"] = 1.5m,
        ["empate"] = 4.2m,
        ["vitoria_visitante"] = 6.0m
    };

    [Fact]
    public void Calcular_should_approve_when_advantage_and_odd_are_within_filter_range()
    {
        AnaliseCalculada resultado = AnaliseDePartidaCalculator.Calcular(
            LigaDeTeste, HistoricoCasa, HistoricoVisitante, CriarCotacao(1.5m), Odds1X2ComMargem, "vitoria_casa");

        resultado.Aprovada.Should().BeTrue();
        resultado.Motivo.Should().BeNull();
        resultado.Vantagem.Should().Be(resultado.ProbDixonColes - resultado.ProbImplicitaDaOdd);
    }

    [Fact]
    public void Calcular_should_reject_with_motivo_when_odd_is_outside_filter_range()
    {
        var odds = new Dictionary<string, decimal>(Odds1X2ComMargem) { ["vitoria_casa"] = 3.0m };

        AnaliseCalculada resultado = AnaliseDePartidaCalculator.Calcular(
            LigaDeTeste, HistoricoCasa, HistoricoVisitante, CriarCotacao(3.0m), odds, "vitoria_casa");

        resultado.Aprovada.Should().BeFalse();
        resultado.Motivo.Should().NotBeNull().And.Contain("Odd");
    }

    [Fact]
    public void Calcular_should_derive_probImplicitaDaOdd_with_margin_removed_instead_of_raw_inverse_of_odd()
    {
        // 1/1.5 = 0.6667 bruto, mas normalizado pela soma de implicitas do grupo (1.0714) fica 28/45 ≈ 0.6222 —
        // menor que o bruto, exatamente o comportamento esperado: sem isso o edge fica inflado pela margem da casa.
        AnaliseCalculada resultado = AnaliseDePartidaCalculator.Calcular(
            LigaDeTeste, HistoricoCasa, HistoricoVisitante, CriarCotacao(1.5m), Odds1X2ComMargem, "vitoria_casa");

        decimal implicitaBruta = 1m / 1.5m;
        resultado.ProbImplicitaDaOdd.Should().BeLessThan(implicitaBruta);
        resultado.ProbImplicitaDaOdd.Should().BeApproximately(0.6222m, 0.0005m);
    }

    [Fact]
    public void Calcular_should_throw_when_group_market_odd_is_missing()
    {
        var oddsIncompletas = new Dictionary<string, decimal> { ["vitoria_casa"] = 1.5m };

        Action act = () => AnaliseDePartidaCalculator.Calcular(
            LigaDeTeste, HistoricoCasa, HistoricoVisitante, CriarCotacao(1.5m), oddsIncompletas, "vitoria_casa");

        act.Should().Throw<InvalidOperationException>();
    }
}
