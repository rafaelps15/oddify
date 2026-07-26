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
}
