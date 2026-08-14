using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.ApostasMultiplas;

public sealed class ApostaMultiplaAnuladaDomainEvent(Guid apostaMultiplaId) : DomainEvent
{
    public Guid ApostaMultiplaId { get; } = apostaMultiplaId;
}
