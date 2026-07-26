namespace Oddify.Modules.Fixtures.Domain.Ligas;

public interface ILigaConfiguradaRepository
{
    Task<LigaConfigurada?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LigaConfigurada?> GetByIdExternoAsync(string idExterno, CancellationToken cancellationToken = default);

    void Insert(LigaConfigurada liga);
}
