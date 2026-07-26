namespace Oddify.Modules.Fixtures.Domain.Partidas;

public interface IPartidaRepository
{
    Task<Partida?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Partida?> GetByIdExternoAsync(string idExterno, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Partida>> ListarAgendadasEntreAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken = default);

    void Insert(Partida partida);
}
