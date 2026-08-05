namespace Oddify.Modules.Apostas.Domain.Bancas;

public interface IBancaRepository
{
    // Sempre filtra por usuarioId — nunca um GetAsync(id) puro. Isolamento multi-tenant real, não
    // decorativo: uma banca de outro usuário resulta no mesmo NotFound de uma banca inexistente.
    Task<Banca?> GetAsync(Guid id, Guid usuarioId, CancellationToken cancellationToken = default);

    // Usado pelo EmailVerifiedIntegrationEventConsumer pra não criar uma segunda banca inicial
    // se o outbox do módulo Users reprocessar o evento de verificação de e-mail.
    Task<bool> ExistsForUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    void Insert(Banca banca);
}
