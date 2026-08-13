namespace Oddify.Modules.Apostas.Domain.PassosDaJornada;

public interface IPassoDaJornadaRepository
{
    Task<PassoDaJornada?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    // O passo EmAberto mais recente de um Numero — é sempre o que uma liquidação em andamento
    // está avaliando (Numero não é único por jornada, ver comentário em PassoDaJornada).
    Task<PassoDaJornada?> GetEmAbertoPorJornadaAsync(Guid jornadaId, CancellationToken cancellationToken = default);

    void Insert(PassoDaJornada passoDaJornada);
}
