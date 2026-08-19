using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.PassosDaJornada;

public sealed class PassoDaJornadaAvancouDomainEvent(Guid passoDaJornadaId, Guid jornadaId, decimal valorResultante) : DomainEvent
{
    public Guid PassoDaJornadaId { get; init; } = passoDaJornadaId;

    public Guid JornadaId { get; init; } = jornadaId;

    public decimal ValorResultante { get; init; } = valorResultante;
}
