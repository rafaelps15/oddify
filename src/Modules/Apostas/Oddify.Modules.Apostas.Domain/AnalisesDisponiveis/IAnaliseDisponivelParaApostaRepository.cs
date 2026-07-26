namespace Oddify.Modules.Apostas.Domain.AnalisesDisponiveis;

public interface IAnaliseDisponivelParaApostaRepository
{
    Task<AnaliseDisponivelParaAposta?> GetAsync(Guid analiseId, CancellationToken cancellationToken = default);

    void Insert(AnaliseDisponivelParaAposta analiseDisponivel);
}
