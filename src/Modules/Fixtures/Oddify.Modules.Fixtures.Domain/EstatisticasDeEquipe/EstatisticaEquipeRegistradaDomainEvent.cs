using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.EstatisticasDeEquipe;

public sealed class EstatisticaEquipeRegistradaDomainEvent(Guid estatisticaEquipeId) : DomainEvent
{
    public Guid EstatisticaEquipeId { get; } = estatisticaEquipeId;
}
