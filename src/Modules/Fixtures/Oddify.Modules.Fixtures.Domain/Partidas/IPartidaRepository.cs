namespace Oddify.Modules.Fixtures.Domain.Partidas;

public interface IPartidaRepository
{
    Task<Partida?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(Partida partida);
}
