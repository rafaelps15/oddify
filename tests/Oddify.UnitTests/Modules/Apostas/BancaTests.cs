using FluentAssertions;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;

namespace Oddify.UnitTests.Modules.Apostas;

public sealed class BancaTests
{
    private static Banca CriarBanca(decimal saldoInicial = 1000m, decimal percentualPorEntrada = 0.05m) =>
        Banca.Create(
            Guid.NewGuid(),
            "Banca principal",
            saldoInicial,
            percentualPorEntrada,
            PerfilDeRisco.Moderado,
            modoPaperTrading: true,
            FinalidadeDaBanca.Principal,
            DateTime.UtcNow);

    [Fact]
    public void RegistrarMovimentacao_should_add_positive_delta()
    {
        Banca banca = CriarBanca();

        banca.RegistrarMovimentacao(150m, TipoDeMovimentacao.Deposito, apostaMultiplaId: null, DateTime.UtcNow);

        banca.SaldoAtual.Should().Be(1150m);
    }

    [Fact]
    public void RegistrarMovimentacao_should_subtract_when_delta_is_negative()
    {
        Banca banca = CriarBanca();

        banca.RegistrarMovimentacao(-50m, TipoDeMovimentacao.Estorno, apostaMultiplaId: null, DateTime.UtcNow);

        banca.SaldoAtual.Should().Be(950m);
    }

    [Fact]
    public void RegistrarMovimentacao_should_return_movimentacao_with_saldo_apos_movimentacao()
    {
        Banca banca = CriarBanca();

        MovimentacaoDaBanca movimentacao = banca.RegistrarMovimentacao(150m, TipoDeMovimentacao.Deposito, apostaMultiplaId: null, DateTime.UtcNow);

        movimentacao.SaldoAposMovimentacao.Should().Be(1150m);
        movimentacao.BancaId.Should().Be(banca.Id);
    }

    [Fact]
    public void ValorDaUnidade_should_be_derived_from_saldo_atual_and_percentual()
    {
        Banca banca = CriarBanca(saldoInicial: 1000m, percentualPorEntrada: 0.05m);

        banca.ValorDaUnidade.Should().Be(50m);

        banca.RegistrarMovimentacao(1000m, TipoDeMovimentacao.Deposito, apostaMultiplaId: null, DateTime.UtcNow);

        banca.ValorDaUnidade.Should().Be(100m);
    }

    [Fact]
    public void Create_should_set_perfil_de_risco()
    {
        Banca banca = CriarBanca();

        banca.PerfilDeRisco.Should().Be(PerfilDeRisco.Moderado);
    }
}
