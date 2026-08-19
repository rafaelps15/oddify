namespace Oddify.Modules.Analise.Domain.Fixtures;

public interface IPartidaRepository
{
    Task<Partida?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    // Últimos `quantidade` jogos ENCERRADOS (GolsCasa/GolsVisitante já preenchidos) em que a
    // equipe jogou em casa ou fora, mais recentes primeiro — usado por HistoricoDeEquipeCalculator
    // pra computar a média de gols feitos/sofridos localmente (AnalisarPartidaCommandHandler).
    Task<IReadOnlyCollection<Partida>> GetRecentesPorEquipeAsync(
        Guid equipeId,
        int quantidade,
        CancellationToken cancellationToken = default);

    void Insert(Partida partida);
}
