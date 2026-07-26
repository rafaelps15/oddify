using Oddify.Common.Application.EventBus;

namespace Oddify.Modules.Fixtures.IntegrationEvents;

public sealed class PartidaEncerradaIntegrationEvent(
    Guid id,
    DateTime occurredOnUtc,
    Guid partidaId,
    int golsCasa,
    int golsVisitante) : IntegrationEvent(id, occurredOnUtc)
{
    public Guid PartidaId { get; } = partidaId;

    public int GolsCasa { get; } = golsCasa;

    public int GolsVisitante { get; } = golsVisitante;
}
