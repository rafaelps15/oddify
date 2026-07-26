using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Jogadores;

public sealed class JogadorCriadoDomainEvent(Guid jogadorId) : DomainEvent
{
    public Guid JogadorId { get; } = jogadorId;
}
