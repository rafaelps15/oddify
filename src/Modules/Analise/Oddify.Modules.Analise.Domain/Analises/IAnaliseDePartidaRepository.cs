namespace Oddify.Modules.Analise.Domain.Analises;

public interface IAnaliseDePartidaRepository
{
    Task<AnaliseDePartida?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(AnaliseDePartida analise);
}
