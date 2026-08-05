using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;

public sealed class MovimentacaoDaBanca : Entity
{
    private MovimentacaoDaBanca()
    {
    }

    public Guid Id { get; private set; }

    public Guid BancaId { get; private set; }

    public TipoDeMovimentacao Tipo { get; private set; }

    public decimal Valor { get; private set; }

    public decimal SaldoAposMovimentacao { get; private set; }

    public Guid? ApostaMultiplaId { get; private set; }

    public DateTime CriadaEmUtc { get; private set; }

    public static MovimentacaoDaBanca Create(
        Guid bancaId,
        TipoDeMovimentacao tipo,
        decimal valor,
        decimal saldoAposMovimentacao,
        Guid? apostaMultiplaId,
        DateTime criadaEmUtc)
    {
        var movimentacao = new MovimentacaoDaBanca
        {
            Id = Guid.NewGuid(),
            BancaId = bancaId,
            Tipo = tipo,
            Valor = valor,
            SaldoAposMovimentacao = saldoAposMovimentacao,
            ApostaMultiplaId = apostaMultiplaId,
            CriadaEmUtc = criadaEmUtc
        };

        movimentacao.Raise(new MovimentacaoDaBancaCriadaDomainEvent(movimentacao.Id));

        return movimentacao;
    }
}
