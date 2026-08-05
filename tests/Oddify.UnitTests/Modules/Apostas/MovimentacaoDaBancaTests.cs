using FluentAssertions;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;

namespace Oddify.UnitTests.Modules.Apostas;

public sealed class MovimentacaoDaBancaTests
{
    [Fact]
    public void Create_should_set_properties_and_raise_MovimentacaoDaBancaCriadaDomainEvent()
    {
        var bancaId = Guid.NewGuid();
        var apostaMultiplaId = Guid.NewGuid();
        DateTime agora = DateTime.UtcNow;

        var movimentacao = MovimentacaoDaBanca.Create(
            bancaId, TipoDeMovimentacao.Liquidacao, valor: 150m, saldoAposMovimentacao: 1150m, apostaMultiplaId, agora);

        movimentacao.BancaId.Should().Be(bancaId);
        movimentacao.Tipo.Should().Be(TipoDeMovimentacao.Liquidacao);
        movimentacao.Valor.Should().Be(150m);
        movimentacao.SaldoAposMovimentacao.Should().Be(1150m);
        movimentacao.ApostaMultiplaId.Should().Be(apostaMultiplaId);
        movimentacao.CriadaEmUtc.Should().Be(agora);
        movimentacao.DomainEvents.Should().ContainSingle(e => e is MovimentacaoDaBancaCriadaDomainEvent);
    }

    [Fact]
    public void Create_should_allow_null_ApostaMultiplaId_for_deposito()
    {
        var movimentacao = MovimentacaoDaBanca.Create(
            Guid.NewGuid(), TipoDeMovimentacao.Deposito, valor: 500m, saldoAposMovimentacao: 1500m, apostaMultiplaId: null, DateTime.UtcNow);

        movimentacao.Tipo.Should().Be(TipoDeMovimentacao.Deposito);
        movimentacao.ApostaMultiplaId.Should().BeNull();
    }
}
