using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.EstatisticasDeJogador;

public sealed class EstatisticaJogadorRegistradaDomainEvent(Guid estatisticaJogadorId) : DomainEvent
{
    public Guid EstatisticaJogadorId { get; } = estatisticaJogadorId;
}
