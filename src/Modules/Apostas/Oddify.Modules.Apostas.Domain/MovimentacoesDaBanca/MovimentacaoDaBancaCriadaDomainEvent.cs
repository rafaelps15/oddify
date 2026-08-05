using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;

public sealed class MovimentacaoDaBancaCriadaDomainEvent(Guid movimentacaoDaBancaId) : DomainEvent
{
    public Guid MovimentacaoDaBancaId { get; } = movimentacaoDaBancaId;
}
