using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Partidas;

public sealed class PartidaReagendadaDomainEvent(Guid partidaId, DateTime novaDataUtc) : DomainEvent
{
    public Guid PartidaId { get; } = partidaId;

    public DateTime NovaDataUtc { get; } = novaDataUtc;
}
