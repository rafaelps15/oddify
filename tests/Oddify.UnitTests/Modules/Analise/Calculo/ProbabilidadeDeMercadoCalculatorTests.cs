using FluentAssertions;
using Oddify.Modules.Analise.Application.Calculo;

namespace Oddify.UnitTests.Modules.Analise.Calculo;

public sealed class ProbabilidadeDeMercadoCalculatorTests
{
    // Matriz 3x3 controlada manualmente (não derivada de Poisson) para tornar as somas esperadas óbvias.
    // Linha = gols da casa (0..2), coluna = gols do visitante (0..2).
    // CA1814: uma matriz bidimensional é a representação natural de uma matriz de placares (gols da casa x gols do visitante).
#pragma warning disable CA1814
    private static decimal[,] MatrizDeTeste() => new decimal[,]
    {
        { 0.10m, 0.05m, 0.05m }, // casa 0 x visitante 0,1,2
        { 0.20m, 0.10m, 0.05m }, // casa 1 x visitante 0,1,2
        { 0.30m, 0.10m, 0.05m }, // casa 2 x visitante 0,1,2
    };
#pragma warning restore CA1814

    [Fact]
    public void Calcular_should_sum_to_one_across_vitoria_casa_empate_e_vitoria_visitante()
    {
        decimal[,] matriz = MatrizDeTeste();

        decimal probCasa = ProbabilidadeDeMercadoCalculator.Calcular(matriz, "vitoria_casa");
        decimal probEmpate = ProbabilidadeDeMercadoCalculator.Calcular(matriz, "empate");
        decimal probVisitante = ProbabilidadeDeMercadoCalculator.Calcular(matriz, "vitoria_visitante");

        (probCasa + probEmpate + probVisitante).Should().BeApproximately(1m, 0.0001m);
    }

    [Fact]
    public void Calcular_should_return_expected_probabilidade_de_empate()
    {
        decimal[,] matriz = MatrizDeTeste();

        // empate: (0,0) + (1,1) + (2,2) = 0.10 + 0.10 + 0.05 = 0.25
        decimal probEmpate = ProbabilidadeDeMercadoCalculator.Calcular(matriz, "empate");

        probEmpate.Should().Be(0.25m);
    }

    [Fact]
    public void Calcular_should_sum_to_one_across_over_and_under_do_mesmo_linha()
    {
        decimal[,] matriz = MatrizDeTeste();

        decimal probOver = ProbabilidadeDeMercadoCalculator.Calcular(matriz, "over_1_5");
        decimal probUnder = ProbabilidadeDeMercadoCalculator.Calcular(matriz, "under_1_5");

        (probOver + probUnder).Should().BeApproximately(1m, 0.0001m);
    }

    [Fact]
    public void Calcular_should_sum_to_one_across_ambos_marcam_e_complemento()
    {
        decimal[,] matriz = MatrizDeTeste();

        decimal probAmbosMarcam = ProbabilidadeDeMercadoCalculator.Calcular(matriz, "ambos_marcam");
        decimal probAmbosMarcamNao = ProbabilidadeDeMercadoCalculator.Calcular(matriz, "ambos_marcam_nao");

        (probAmbosMarcam + probAmbosMarcamNao).Should().BeApproximately(1m, 0.0001m);
    }
}
