using FluentAssertions;
using Oddify.Modules.Analise.Application.Calculo;

namespace Oddify.UnitTests.Modules.Analise.Calculo;

public sealed class DixonColesCorrecaoTests
{
    [Fact]
    public void Aplicar_should_keep_matrix_summing_to_approximately_one()
    {
        decimal[,] matrizPura = PoissonCalculator.MatrizDePlacares(1.4m, 1.1m);

        decimal[,] corrigida = DixonColesCorrecao.Aplicar(matrizPura, 1.4m, 1.1m);

        decimal soma = 0m;

        foreach (decimal valor in corrigida)
        {
            soma += valor;
        }

        soma.Should().BeApproximately(1m, 0.01m);
    }

    [Fact]
    public void Aplicar_should_only_change_the_four_low_score_cells_proportionally()
    {
        decimal[,] matrizPura = PoissonCalculator.MatrizDePlacares(1.4m, 1.1m);

        decimal[,] corrigida = DixonColesCorrecao.Aplicar(matrizPura, 1.4m, 1.1m);

        // Uma célula fora das 4 corrigidas (ex: 5x5) muda apenas pela renormalização global,
        // então a razão entre ela e outra célula não corrigida (ex: 2x3) deve permanecer igual.
        decimal razaoOriginal = matrizPura[5, 5] / matrizPura[2, 3];
        decimal razaoCorrigida = corrigida[5, 5] / corrigida[2, 3];

        razaoCorrigida.Should().BeApproximately(razaoOriginal, 0.0001m);
    }

    [Fact]
    public void Aplicar_should_reduce_the_zero_zero_cell_when_rho_is_negative_and_lambdas_are_positive()
    {
        decimal[,] matrizPura = PoissonCalculator.MatrizDePlacares(1.4m, 1.1m);

        decimal[,] corrigida = DixonColesCorrecao.Aplicar(matrizPura, 1.4m, 1.1m, rho: -0.1m);

        // tau(0,0) = 1 - lambdaCasa*lambdaVisitante*rho > 1 quando rho é negativo,
        // então a célula 0x0 deve crescer relativamente às demais (antes da renormalização
        // ela cresce; a renormalização divide tudo pelo mesmo fator, então a razão para uma
        // célula não corrigida deve ser maior que 1).
        decimal razaoAntes = matrizPura[0, 0] / matrizPura[5, 5];
        decimal razaoDepois = corrigida[0, 0] / corrigida[5, 5];

        razaoDepois.Should().BeGreaterThan(razaoAntes);
    }
}
