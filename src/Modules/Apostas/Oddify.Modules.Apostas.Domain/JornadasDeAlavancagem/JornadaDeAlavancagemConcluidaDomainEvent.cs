using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

public sealed class JornadaDeAlavancagemConcluidaDomainEvent(Guid jornadaDeAlavancagemId) : DomainEvent
{
    public Guid JornadaDeAlavancagemId { get; init; } = jornadaDeAlavancagemId;
}
