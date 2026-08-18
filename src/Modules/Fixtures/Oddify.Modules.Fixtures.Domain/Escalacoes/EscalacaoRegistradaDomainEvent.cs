using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Escalacoes;

public sealed class EscalacaoRegistradaDomainEvent(Guid escalacaoId) : DomainEvent
{
    public Guid EscalacaoId { get; } = escalacaoId;
}
