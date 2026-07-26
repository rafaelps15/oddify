using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.ApostasMultiplas;

public sealed class ApostaMultiplaLiquidadaDomainEvent(Guid apostaMultiplaId, decimal lucroOuPerda) : DomainEvent
{
    public Guid ApostaMultiplaId { get; } = apostaMultiplaId;

    public decimal LucroOuPerda { get; } = lucroOuPerda;
}
