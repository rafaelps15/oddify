using FluentAssertions;
using Oddify.Modules.Analise.Application.Calculo;

namespace Oddify.UnitTests.Modules.Analise.Calculo;

public sealed class PoissonCalculatorTests
{
    [Fact]
    public void CalcularLambdas_should_return_media_da_liga_ajustada_pelo_fator_casa_when_times_estao_na_media()
    {
        (decimal lambdaCasa, decimal lambdaVisitante) = PoissonCalculator.CalcularLambdas(
            mediaDeGolsDaLiga: 2.5m,
            fatorCasa: 1.1m,
            mediaGolsFeitosCasa: 2.5m,
            mediaGolsSofridosCasa: 2.5m,
            mediaGolsFeitosVisitante: 2.5m,
            mediaGolsSofridosVisitante: 2.5m);

        lambdaCasa.Should().Be(2.5m * 1.1m);
        lambdaVisitante.Should().Be(2.5m);
    }

    [Fact]
    public void CalcularLambdas_should_return_lambda_maior_when_time_ataca_acima_da_media_e_adversario_defende_mal()
    {
        (decimal lambdaCasa, _) = PoissonCalculator.CalcularLambdas(
            mediaDeGolsDaLiga: 2.5m,
            fatorCasa: 1m,
            mediaGolsFeitosCasa: 3.5m,
            mediaGolsSofridosCasa: 2.5m,
            mediaGolsFeitosVisitante: 2.5m,
            mediaGolsSofridosVisitante: 3.5m);

        lambdaCasa.Should().BeGreaterThan(2.5m);
    }

    [Theory]
    [InlineData(0.8)]
    [InlineData(1.5)]
    [InlineData(3.0)]
    public void ProbabilidadePoisson_should_sum_to_approximately_one_across_the_matrix_window(double lambda)
    {
        decimal soma = 0m;

        for (int k = 0; k <= PoissonCalculator.MaxGolsNaMatriz; k++)
        {
            soma += PoissonCalculator.ProbabilidadePoisson((decimal)lambda, k);
        }

        soma.Should().BeApproximately(1m, 0.01m);
    }

    [Fact]
    public void ProbabilidadePoisson_should_return_e_pow_minus_lambda_when_k_is_zero()
    {
        decimal resultado = PoissonCalculator.ProbabilidadePoisson(1m, 0);

        resultado.Should().BeApproximately((decimal)Math.Exp(-1), 0.0001m);
    }

    [Fact]
    public void MatrizDePlacares_should_have_all_non_negative_cells_summing_to_approximately_one()
    {
        decimal[,] matriz = PoissonCalculator.MatrizDePlacares(1.4m, 1.1m);

        decimal soma = 0m;

        for (int i = 0; i < matriz.GetLength(0); i++)
        {
            for (int j = 0; j < matriz.GetLength(1); j++)
            {
                matriz[i, j].Should().BeGreaterThanOrEqualTo(0m);
                soma += matriz[i, j];
            }
        }

        soma.Should().BeApproximately(1m, 0.01m);
    }
}
