using FluentAssertions;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.UnitTests.Modules.Apostas;

public sealed class BancaTests
{
    private static Banca CriarBanca(decimal saldoInicial = 1000m, decimal percentualPorEntrada = 0.05m) =>
        Banca.Create(Guid.NewGuid(), "Banca principal", saldoInicial, percentualPorEntrada, modoPaperTrading: true, DateTime.UtcNow);

    [Fact]
    public void AjustarSaldo_should_add_positive_delta()
    {
        Banca banca = CriarBanca();

        banca.AjustarSaldo(150m, DateTime.UtcNow);

        banca.SaldoAtual.Should().Be(1150m);
    }

    [Fact]
    public void AjustarSaldo_should_subtract_when_delta_is_negative()
    {
        Banca banca = CriarBanca();

        banca.AjustarSaldo(-50m, DateTime.UtcNow);

        banca.SaldoAtual.Should().Be(950m);
    }

    [Fact]
    public void ValorDaUnidade_should_be_derived_from_saldo_atual_and_percentual()
    {
        Banca banca = CriarBanca(saldoInicial: 1000m, percentualPorEntrada: 0.05m);

        banca.ValorDaUnidade.Should().Be(50m);

        banca.AjustarSaldo(1000m, DateTime.UtcNow);

        banca.ValorDaUnidade.Should().Be(100m);
    }
}
