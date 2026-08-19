namespace Oddify.Modules.Analise.Domain.Fixtures;

public interface ILigaRepository
{
    Task<Liga?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(Liga liga);
}
