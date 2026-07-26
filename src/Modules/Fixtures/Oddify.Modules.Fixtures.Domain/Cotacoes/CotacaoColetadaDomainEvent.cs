using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Cotacoes;

public sealed class CotacaoColetadaDomainEvent(Guid cotacaoId) : DomainEvent
{
    public Guid CotacaoId { get; } = cotacaoId;
}
