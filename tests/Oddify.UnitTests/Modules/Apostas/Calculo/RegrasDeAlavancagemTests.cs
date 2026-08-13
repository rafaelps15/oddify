using FluentAssertions;
using Oddify.Modules.Apostas.Application.Calculo;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

namespace Oddify.UnitTests.Modules.Apostas.Calculo;

public sealed class RegrasDeAlavancagemTests
{
    [Theory]
    [InlineData(3, 75)]
    [InlineData(4, 100)]
    public void CalcularBancaMinima_should_be_numero_de_fracoes_times_unidade_de_entrada(int numeroDeFracoes, decimal esperado)
    {
        RegrasDeAlavancagem.CalcularBancaMinima(numeroDeFracoes).Should().Be(esperado);
    }

    [Fact]
    public void CalcularProbabilidadeDeAvancoPorPasso_should_match_spec_value_for_three_fracoes()
    {
        // p=0.70 por entrada, avanço exige >=2 de 3: 1 - 0.3^3 - 3*0.7*0.3^2 = 0.784 (spec 17.12: ~78%).
        decimal probabilidade = RegrasDeAlavancagem.CalcularProbabilidadeDeAvancoPorPasso(3);

        probabilidade.Should().BeApproximately(0.784m, 0.0001m);
    }

    [Fact]
    public void CalcularProbabilidadeDeAvancoPorPasso_should_match_spec_value_for_four_fracoes()
    {
        // 1 - 0.3^4 - 4*0.7*0.3^3 = 0.9163 (spec 17.12: ~91.6%).
        decimal probabilidade = RegrasDeAlavancagem.CalcularProbabilidadeDeAvancoPorPasso(4);

        probabilidade.Should().BeApproximately(0.9163m, 0.0001m);
    }

    [Fact]
    public void CalcularProbabilidadeDeAvancoPorPasso_should_increase_with_more_fracoes()
    {
        // Mais frações == mais chances de bater o limiar fixo de 2 vitórias.
        decimal comTresFracoes = RegrasDeAlavancagem.CalcularProbabilidadeDeAvancoPorPasso(3);
        decimal comQuatroFracoes = RegrasDeAlavancagem.CalcularProbabilidadeDeAvancoPorPasso(4);

        comQuatroFracoes.Should().BeGreaterThan(comTresFracoes);
    }

    [Theory]
    [InlineData(FaixaDeMeta.Dobrar, 3, 3, 2)]
    [InlineData(FaixaDeMeta.Triplicar, 3, 5, 3)]
    [InlineData(FaixaDeMeta.CincoVezes, 4, 8, 5)]
    public void ObterInfo_should_return_catalog_entry_for_each_faixa(
        FaixaDeMeta faixa, int numeroDeFracoesEsperado, int totalDePassosEsperado, int multiplicadorEsperado)
    {
        RegrasDeAlavancagem.FaixaDeMetaInfo info = RegrasDeAlavancagem.ObterInfo(faixa);

        info.NumeroDeFracoes.Should().Be(numeroDeFracoesEsperado);
        info.TotalDePassos.Should().Be(totalDePassosEsperado);
        info.Multiplicador.Should().Be(multiplicadorEsperado);
    }

    [Theory]
    [InlineData(FaixaDeMeta.Dobrar)]
    [InlineData(FaixaDeMeta.Triplicar)]
    [InlineData(FaixaDeMeta.CincoVezes)]
    public void CalcularProbabilidadeDeConclusao_should_equal_avanco_por_passo_raised_to_total_de_passos(FaixaDeMeta faixa)
    {
        RegrasDeAlavancagem.FaixaDeMetaInfo info = RegrasDeAlavancagem.ObterInfo(faixa);
        decimal avancoPorPasso = RegrasDeAlavancagem.CalcularProbabilidadeDeAvancoPorPasso(info.NumeroDeFracoes);
        decimal esperado = (decimal)Math.Pow((double)avancoPorPasso, info.TotalDePassos);

        RegrasDeAlavancagem.CalcularProbabilidadeDeConclusao(faixa).Should().Be(esperado);
    }

    [Fact]
    public void CalcularProbabilidadeDeConclusao_should_be_within_spec_ballpark_for_each_faixa()
    {
        // Spec 17.12: Dobrar ~48%, Triplicar ~29%, CincoVezes ~49%.
        RegrasDeAlavancagem.CalcularProbabilidadeDeConclusao(FaixaDeMeta.Dobrar).Should().BeApproximately(0.48m, 0.01m);
        RegrasDeAlavancagem.CalcularProbabilidadeDeConclusao(FaixaDeMeta.Triplicar).Should().BeApproximately(0.29m, 0.01m);
        RegrasDeAlavancagem.CalcularProbabilidadeDeConclusao(FaixaDeMeta.CincoVezes).Should().BeApproximately(0.49m, 0.01m);
    }
}
