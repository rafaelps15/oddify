using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.EscalacoesDeJogador;

public sealed class EscalacaoJogador : Entity
{
    private EscalacaoJogador()
    {
    }

    public Guid Id { get; private set; }

    public Guid EscalacaoId { get; private set; }

    public Guid JogadorId { get; private set; }

    public bool Titular { get; private set; }

    public string Posicao { get; private set; }

    public int? Numero { get; private set; }

    public static EscalacaoJogador Create(Guid escalacaoId, Guid jogadorId, bool titular, string posicao, int? numero)
    {
        var escalacaoJogador = new EscalacaoJogador
        {
            Id = Guid.NewGuid(),
            EscalacaoId = escalacaoId,
            JogadorId = jogadorId,
            Titular = titular,
            Posicao = posicao,
            Numero = numero
        };

        escalacaoJogador.Raise(new EscalacaoJogadorRegistradaDomainEvent(escalacaoJogador.Id));

        return escalacaoJogador;
    }
}
