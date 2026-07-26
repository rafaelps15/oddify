namespace Oddify.Modules.Fixtures.Domain.EstatisticasDeEquipe;

public interface IEstatisticaEquipeRepository
{
    Task<EstatisticaEquipe?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(EstatisticaEquipe estatistica);
}
