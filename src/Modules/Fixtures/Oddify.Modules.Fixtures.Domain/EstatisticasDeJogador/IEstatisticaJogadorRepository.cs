namespace Oddify.Modules.Fixtures.Domain.EstatisticasDeJogador;

public interface IEstatisticaJogadorRepository
{
    Task<EstatisticaJogador?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(EstatisticaJogador estatistica);
}
