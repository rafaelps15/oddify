using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.PassosDaJornada;

public sealed class PassoDaJornadaCriadoDomainEvent(Guid passoDaJornadaId, Guid jornadaId) : DomainEvent
{
    public Guid PassoDaJornadaId { get; init; } = passoDaJornadaId;

    public Guid JornadaId { get; init; } = jornadaId;
}
