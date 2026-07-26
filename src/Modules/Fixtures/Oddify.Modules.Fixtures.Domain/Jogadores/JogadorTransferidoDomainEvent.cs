using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Jogadores;

public sealed class JogadorTransferidoDomainEvent(Guid jogadorId, Guid novaEquipeId) : DomainEvent
{
    public Guid JogadorId { get; } = jogadorId;

    public Guid NovaEquipeId { get; } = novaEquipeId;
}
