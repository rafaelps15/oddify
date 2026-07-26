namespace Oddify.Modules.Fixtures.Domain.Jogadores;

public interface IJogadorRepository
{
    Task<Jogador?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(Jogador jogador);
}
