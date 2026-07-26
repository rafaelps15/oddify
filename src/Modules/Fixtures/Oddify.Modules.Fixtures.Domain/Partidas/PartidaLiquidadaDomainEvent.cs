using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Partidas;

public sealed class PartidaLiquidadaDomainEvent(Guid partidaId) : DomainEvent
{
    public Guid PartidaId { get; } = partidaId;
}
