using FluentAssertions;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.UnitTests.Modules.Apostas;

public sealed class BancaTests
{
    [Fact]
    public void AjustarSaldo_should_add_positive_delta()
    {
        var banca = Banca.Create(saldoInicial: 1000m, modoPaperTrading: true);

        banca.AjustarSaldo(150m);

        banca.SaldoAtual.Should().Be(1150m);
    }

    [Fact]
    public void AjustarSaldo_should_subtract_when_delta_is_negative()
    {
        var banca = Banca.Create(saldoInicial: 1000m, modoPaperTrading: true);

        banca.AjustarSaldo(-50m);

        banca.SaldoAtual.Should().Be(950m);
    }
}
