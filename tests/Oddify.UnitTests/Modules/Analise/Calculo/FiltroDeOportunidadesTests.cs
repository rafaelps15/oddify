using FluentAssertions;
using Oddify.Modules.Analise.Application.Calculo;

namespace Oddify.UnitTests.Modules.Analise.Calculo;

public sealed class FiltroDeOportunidadesTests
{
    [Fact]
    public void Avaliar_should_approve_when_all_conditions_pass()
    {
        (bool aprovada, string? motivo) = FiltroDeOportunidades.Avaliar(vantagem: 0.05m, odd: 1.5m, amostraDeJogos: 10, ligaCalibrada: true);

        aprovada.Should().BeTrue();
        motivo.Should().BeNull();
    }

    [Fact]
    public void Avaliar_should_reject_with_vantagem_motivo_when_vantagem_below_minimum()
    {
        (bool aprovada, string? motivo) = FiltroDeOportunidades.Avaliar(vantagem: 0.03m, odd: 1.5m, amostraDeJogos: 10, ligaCalibrada: true);

        aprovada.Should().BeFalse();
        motivo.Should().Contain("Vantagem");
    }

    [Theory]
    [InlineData(1.30)]
    [InlineData(1.80)]
    public void Avaliar_should_reject_with_odd_motivo_when_odd_outside_range(double odd)
    {
        (bool aprovada, string? motivo) = FiltroDeOportunidades.Avaliar(vantagem: 0.05m, odd: (decimal)odd, amostraDeJogos: 10, ligaCalibrada: true);

        aprovada.Should().BeFalse();
        motivo.Should().Contain("Odd");
    }

    [Fact]
    public void Avaliar_should_reject_with_amostra_motivo_when_sample_below_minimum()
    {
        (bool aprovada, string? motivo) = FiltroDeOportunidades.Avaliar(vantagem: 0.05m, odd: 1.5m, amostraDeJogos: 5, ligaCalibrada: true);

        aprovada.Should().BeFalse();
        motivo.Should().Contain("Amostra");
    }

    [Fact]
    public void Avaliar_should_reject_with_calibrada_motivo_when_liga_not_calibrada()
    {
        (bool aprovada, string? motivo) = FiltroDeOportunidades.Avaliar(vantagem: 0.05m, odd: 1.5m, amostraDeJogos: 10, ligaCalibrada: false);

        aprovada.Should().BeFalse();
        motivo.Should().Contain("calibrada");
    }

    [Fact]
    public void Avaliar_should_concatenate_multiple_reasons_when_several_conditions_fail()
    {
        (bool aprovada, string? motivo) = FiltroDeOportunidades.Avaliar(vantagem: 0.01m, odd: 2.0m, amostraDeJogos: 3, ligaCalibrada: false);

        aprovada.Should().BeFalse();
        motivo.Should().Contain("Vantagem").And.Contain("Odd").And.Contain("Amostra").And.Contain("calibrada");
        motivo.Should().Contain(";");
    }
}
