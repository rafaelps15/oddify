using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.Bancas;

public sealed class BancaCriadaDomainEvent(Guid bancaId) : DomainEvent
{
    public Guid BancaId { get; init; } = bancaId;
}
