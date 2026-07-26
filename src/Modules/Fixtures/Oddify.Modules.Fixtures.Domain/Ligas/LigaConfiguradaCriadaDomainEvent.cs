using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Ligas;

public sealed class LigaConfiguradaCriadaDomainEvent(Guid ligaId) : DomainEvent
{
    public Guid LigaId { get; } = ligaId;
}
