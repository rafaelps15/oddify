using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

public sealed class JornadaDeAlavancagemPassoAvancadoDomainEvent(Guid jornadaDeAlavancagemId, int novoPassoAtual) : DomainEvent
{
    public Guid JornadaDeAlavancagemId { get; init; } = jornadaDeAlavancagemId;

    public int NovoPassoAtual { get; init; } = novoPassoAtual;
}
