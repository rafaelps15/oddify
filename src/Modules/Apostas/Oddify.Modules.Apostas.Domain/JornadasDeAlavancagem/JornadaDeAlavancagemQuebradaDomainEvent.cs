using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

public sealed class JornadaDeAlavancagemQuebradaDomainEvent(Guid jornadaDeAlavancagemId) : DomainEvent
{
    public Guid JornadaDeAlavancagemId { get; init; } = jornadaDeAlavancagemId;
}
