using Oddify.Common.Application.EventBus;

namespace Oddify.Modules.Fixtures.IntegrationEvents;

public sealed class PartidaAgendadaIntegrationEvent(
    Guid id,
    DateTime occurredOnUtc,
    Guid partidaId,
    Guid ligaId,
    Guid equipeCasaId,
    Guid equipeVisitanteId,
    DateTime dataUtc) : IntegrationEvent(id, occurredOnUtc)
{
    public Guid PartidaId { get; } = partidaId;

    public Guid LigaId { get; } = ligaId;

    public Guid EquipeCasaId { get; } = equipeCasaId;

    public Guid EquipeVisitanteId { get; } = equipeVisitanteId;

    public DateTime DataUtc { get; } = dataUtc;
}
