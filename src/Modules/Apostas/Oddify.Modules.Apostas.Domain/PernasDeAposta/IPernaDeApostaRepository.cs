namespace Oddify.Modules.Apostas.Domain.PernasDeAposta;

public interface IPernaDeApostaRepository
{
    Task<PernaDeAposta?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(PernaDeAposta pernaDeAposta);

    Task<IReadOnlyCollection<PernaDeAposta>> GetPorApostaMultiplaAsync(Guid apostaMultiplaId, CancellationToken cancellationToken = default);
}
