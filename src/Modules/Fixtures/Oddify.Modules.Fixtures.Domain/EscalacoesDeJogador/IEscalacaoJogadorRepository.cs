namespace Oddify.Modules.Fixtures.Domain.EscalacoesDeJogador;

public interface IEscalacaoJogadorRepository
{
    Task<EscalacaoJogador?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(EscalacaoJogador escalacaoJogador);
}
