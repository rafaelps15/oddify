namespace Oddify.Modules.Apostas.Domain.Bancas;

public interface IBancaRepository
{
    Task<Banca?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(Banca banca);
}
