using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.Bancas;

public sealed class SaldoDaBancaAjustadoDomainEvent(Guid bancaId, decimal saldoAtual) : DomainEvent
{
    public Guid BancaId { get; init; } = bancaId;

    public decimal SaldoAtual { get; init; } = saldoAtual;
}
