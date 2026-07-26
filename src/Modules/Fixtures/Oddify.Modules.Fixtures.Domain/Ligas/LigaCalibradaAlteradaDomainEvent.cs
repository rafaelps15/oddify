using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Ligas;

public sealed class LigaCalibradaAlteradaDomainEvent(Guid ligaId, bool calibrada) : DomainEvent
{
    public Guid LigaId { get; } = ligaId;

    public bool Calibrada { get; } = calibrada;
}
