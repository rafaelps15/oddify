namespace Oddify.Modules.Analise.Domain.Fixtures;

public interface ICotacaoRepository
{
    Task<Cotacao?> GetMaisRecenteAsync(Guid partidaId, string mercado, CancellationToken cancellationToken = default);

    /// <summary>Todas as cotações já espelhadas para a partida (todos os mercados e casas), mais recentes primeiro.</summary>
    Task<IReadOnlyCollection<Cotacao>> GetPorPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default);

    void Insert(Cotacao cotacao);
}
