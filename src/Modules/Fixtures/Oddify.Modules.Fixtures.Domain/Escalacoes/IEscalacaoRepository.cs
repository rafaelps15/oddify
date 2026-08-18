namespace Oddify.Modules.Fixtures.Domain.Escalacoes;

public interface IEscalacaoRepository
{
    Task<Escalacao?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(Escalacao escalacao);
}
