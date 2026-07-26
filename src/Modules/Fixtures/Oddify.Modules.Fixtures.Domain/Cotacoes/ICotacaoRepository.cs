namespace Oddify.Modules.Fixtures.Domain.Cotacoes;

public interface ICotacaoRepository
{
    Task<Cotacao?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    void Insert(Cotacao cotacao);
}
