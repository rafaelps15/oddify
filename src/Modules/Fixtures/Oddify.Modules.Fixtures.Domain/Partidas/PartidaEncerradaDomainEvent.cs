using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Partidas;

public sealed class PartidaEncerradaDomainEvent(Guid partidaId, int golsCasa, int golsVisitante) : DomainEvent
{
    public Guid PartidaId { get; } = partidaId;

    public int GolsCasa { get; } = golsCasa;

    public int GolsVisitante { get; } = golsVisitante;
}
