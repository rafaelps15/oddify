using FluentAssertions;
using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.UnitTests.Modules.Analise;

public sealed class MercadoResolverTests
{
    [Theory]
    [InlineData("vitoria_casa", 2, 1, true)]
    [InlineData("vitoria_casa", 1, 2, false)]
    [InlineData("vitoria_casa", 1, 1, false)]
    [InlineData("empate", 1, 1, true)]
    [InlineData("empate", 2, 1, false)]
    [InlineData("vitoria_visitante", 0, 1, true)]
    [InlineData("vitoria_visitante", 1, 0, false)]
    [InlineData("ambos_marcam", 1, 1, true)]
    [InlineData("ambos_marcam", 1, 0, false)]
    [InlineData("ambos_marcam", 0, 0, false)]
    [InlineData("ambos_marcam_nao", 0, 0, true)]
    [InlineData("ambos_marcam_nao", 1, 0, true)]
    [InlineData("ambos_marcam_nao", 1, 1, false)]
    [InlineData("over_2_5", 2, 1, true)]
    [InlineData("over_2_5", 1, 1, false)]
    [InlineData("under_2_5", 1, 1, true)]
    [InlineData("under_2_5", 2, 1, false)]
    public void Resolver_should_return_expected_result_for_supported_mercados(string mercado, int golsCasa, int golsVisitante, bool esperado)
    {
        bool resultado = MercadoResolver.Resolver(mercado, golsCasa, golsVisitante);

        resultado.Should().Be(esperado);
    }

    [Fact]
    public void Resolver_should_throw_when_mercado_is_unknown()
    {
        Action act = () => MercadoResolver.Resolver("mercado_inexistente", 1, 1);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("vitoria_casa")]
    [InlineData("empate")]
    [InlineData("vitoria_visitante")]
    public void ObterGrupoDeMercados_should_return_the_three_1x2_outcomes(string mercado)
    {
        IReadOnlyCollection<string> grupo = MercadoResolver.ObterGrupoDeMercados(mercado);

        grupo.Should().BeEquivalentTo("vitoria_casa", "empate", "vitoria_visitante");
    }

    [Theory]
    [InlineData("ambos_marcam")]
    [InlineData("ambos_marcam_nao")]
    public void ObterGrupoDeMercados_should_return_the_two_btts_outcomes(string mercado)
    {
        IReadOnlyCollection<string> grupo = MercadoResolver.ObterGrupoDeMercados(mercado);

        grupo.Should().BeEquivalentTo("ambos_marcam", "ambos_marcam_nao");
    }

    [Theory]
    [InlineData("over_2_5")]
    [InlineData("under_2_5")]
    public void ObterGrupoDeMercados_should_return_the_over_and_under_of_the_same_line(string mercado)
    {
        IReadOnlyCollection<string> grupo = MercadoResolver.ObterGrupoDeMercados(mercado);

        grupo.Should().BeEquivalentTo("over_2_5", "under_2_5");
    }

    [Fact]
    public void ObterGrupoDeMercados_should_not_mix_different_lines()
    {
        IReadOnlyCollection<string> grupo = MercadoResolver.ObterGrupoDeMercados("over_1_5");

        grupo.Should().NotContain("under_2_5");
        grupo.Should().BeEquivalentTo("over_1_5", "under_1_5");
    }

    [Fact]
    public void ObterGrupoDeMercados_should_throw_when_mercado_is_unknown()
    {
        Action act = () => MercadoResolver.ObterGrupoDeMercados("mercado_inexistente");

        act.Should().Throw<ArgumentException>();
    }
}
