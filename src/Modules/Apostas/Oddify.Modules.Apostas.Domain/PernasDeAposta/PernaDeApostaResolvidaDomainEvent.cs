using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.PernasDeAposta;

public sealed class PernaDeApostaResolvidaDomainEvent(Guid pernaDeApostaId, Guid apostaMultiplaId, bool ganhou) : DomainEvent
{
    public Guid PernaDeApostaId { get; init; } = pernaDeApostaId;

    public Guid ApostaMultiplaId { get; init; } = apostaMultiplaId;

    public bool Ganhou { get; init; } = ganhou;
}
