using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.PernasDeAposta;

public sealed class PernaDeApostaAnuladaDomainEvent(Guid pernaDeApostaId, Guid apostaMultiplaId) : DomainEvent
{
    public Guid PernaDeApostaId { get; init; } = pernaDeApostaId;

    public Guid ApostaMultiplaId { get; init; } = apostaMultiplaId;
}
