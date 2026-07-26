using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.ApostasMultiplas;

public sealed class ApostaMultiplaCriadaDomainEvent(Guid apostaMultiplaId) : DomainEvent
{
    public Guid ApostaMultiplaId { get; } = apostaMultiplaId;
}
