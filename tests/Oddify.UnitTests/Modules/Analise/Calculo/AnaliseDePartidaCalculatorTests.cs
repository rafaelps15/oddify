using FluentAssertions;
using Oddify.Modules.Analise.Application.Calculo;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.UnitTests.Modules.Analise.Calculo;

public sealed class AnaliseDePartidaCalculatorTests
{
    private static readonly LigaResponse Liga = new(Guid.NewGuid(), "Liga de Teste", 2.5m, 1.1m, Calibrada: true);

    // Time da casa ataca bem acima da média e o visitante defende mal (e vice-versa) para dar uma
    // margem folgada de vantagem, mesma configuração usada em AnalisarPartidaCommandHandlerTests.
    private static readonly HistoricoDeEquipeResponse HistoricoCasa = new(AmostraDeJogos: 10, MediaGolsFeitos: 3.0m, MediaGolsSofridos: 1.0m);
    private static readonly HistoricoDeEquipeResponse HistoricoVisitante = new(AmostraDeJogos: 10, MediaGolsFeitos: 1.0m, MediaGolsSofridos: 3.0m);

    private static CotacaoResponse CriarCotacao(decimal odd) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "vitoria_casa", odd, "casa-de-apostas", DateTime.UtcNow);

    [Fact]
    public void Calcular_should_approve_when_advantage_and_odd_are_within_filter_range()
    {
        AnaliseCalculada resultado = AnaliseDePartidaCalculator.Calcular(
            Liga, HistoricoCasa, HistoricoVisitante, CriarCotacao(1.5m), "vitoria_casa");

        resultado.Aprovada.Should().BeTrue();
        resultado.Motivo.Should().BeNull();
        resultado.Vantagem.Should().Be(resultado.ProbDixonColes - resultado.ProbImplicitaDaOdd);
    }

    [Fact]
    public void Calcular_should_reject_with_motivo_when_odd_is_outside_filter_range()
    {
        AnaliseCalculada resultado = AnaliseDePartidaCalculator.Calcular(
            Liga, HistoricoCasa, HistoricoVisitante, CriarCotacao(3.0m), "vitoria_casa");

        resultado.Aprovada.Should().BeFalse();
        resultado.Motivo.Should().NotBeNull().And.Contain("Odd");
    }

    [Fact]
    public void Calcular_should_derive_probImplicitaDaOdd_as_inverse_of_odd()
    {
        AnaliseCalculada resultado = AnaliseDePartidaCalculator.Calcular(
            Liga, HistoricoCasa, HistoricoVisitante, CriarCotacao(2.0m), "vitoria_casa");

        resultado.ProbImplicitaDaOdd.Should().Be(0.5m);
    }
}
