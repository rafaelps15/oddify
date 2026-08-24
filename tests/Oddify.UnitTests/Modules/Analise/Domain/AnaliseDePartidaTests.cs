using FluentAssertions;
using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.UnitTests.Modules.Analise.Domain;

public sealed class AnaliseDePartidaTests
{
    private static AnaliseDePartida CriarAnalise(bool aprovadaNoFiltro = true) => AnaliseDePartida.Create(
        Guid.NewGuid(), "vitoria_casa", 0.5m, 0.5m, 0.5m, 0.0m, 1.9m, aprovadaNoFiltro, motivoDoDescarte: null, DateTime.UtcNow.AddHours(-1));

    [Fact]
    public void AtualizarCalculo_should_overwrite_calculated_fields()
    {
        AnaliseDePartida analise = CriarAnalise();
        DateTime atualizadaEmUtc = DateTime.UtcNow;

        analise.AtualizarCalculo(0.61m, 0.63m, 0.55m, 0.08m, 1.85m, aprovadaNoFiltro: true, motivoDoDescarte: null, atualizadaEmUtc);

        analise.ProbPoissonPura.Should().Be(0.61m);
        analise.ProbDixonColes.Should().Be(0.63m);
        analise.ProbImplicitaDaOdd.Should().Be(0.55m);
        analise.Vantagem.Should().Be(0.08m);
        analise.OddDeMercado.Should().Be(1.85m);
        analise.AprovadaNoFiltro.Should().BeTrue();
        analise.MotivoDoDescarte.Should().BeNull();
    }

    [Fact]
    public void AtualizarCalculo_should_reset_claude_evaluation_fields()
    {
        AnaliseDePartida analise = CriarAnalise(aprovadaNoFiltro: true);
        analise.RegistrarDecisaoDoClaude(DecisaoDoClaude.Confirma, "justificativa", "resposta bruta", "v1");

        analise.AtualizarCalculo(0.61m, 0.63m, 0.55m, 0.08m, 1.85m, aprovadaNoFiltro: true, motivoDoDescarte: null, DateTime.UtcNow);

        analise.DecisaoDoClaude.Should().Be(DecisaoDoClaude.NaoAvaliada);
        analise.JustificativaDoClaude.Should().BeNull();
        analise.RespostaLlmBruta.Should().BeNull();
        analise.VersaoDoPrompt.Should().BeNull();
    }

    [Fact]
    public void AtualizarCalculo_should_update_criada_em_utc()
    {
        AnaliseDePartida analise = CriarAnalise();
        DateTime atualizadaEmUtc = DateTime.UtcNow;

        analise.AtualizarCalculo(0.61m, 0.63m, 0.55m, 0.08m, 1.85m, aprovadaNoFiltro: true, motivoDoDescarte: null, atualizadaEmUtc);

        analise.CriadaEmUtc.Should().Be(atualizadaEmUtc);
    }
}
