namespace Oddify.Modules.Analise.Domain.Analises;

public interface IAnaliseDePartidaRepository
{
    Task<AnaliseDePartida?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AnaliseDePartida?> GetPorPartidaEMercadoAsync(Guid partidaId, string mercado, CancellationToken cancellationToken = default);

    void Insert(AnaliseDePartida analise);
}
