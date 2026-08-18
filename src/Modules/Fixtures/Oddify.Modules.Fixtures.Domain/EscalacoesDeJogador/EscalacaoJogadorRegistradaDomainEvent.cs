using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.EscalacoesDeJogador;

public sealed class EscalacaoJogadorRegistradaDomainEvent(Guid escalacaoJogadorId) : DomainEvent
{
    public Guid EscalacaoJogadorId { get; } = escalacaoJogadorId;
}
