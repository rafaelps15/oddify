namespace Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

public interface IJornadaDeAlavancagemRepository
{
    Task<JornadaDeAlavancagem?> GetAsync(Guid id, Guid usuarioId, CancellationToken cancellationToken = default);

    // Sem escopo de usuário — só pro fluxo interno disparado pela liquidação de uma aposta
    // (AvaliarPassoDaJornadaCommandHandler), que já chega no PassoDaJornada/JornadaId por uma
    // cadeia de ids internos confiável, sem um usuário autenticado direto na requisição.
    Task<JornadaDeAlavancagem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<JornadaDeAlavancagem?> GetEmAndamentoAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    void Insert(JornadaDeAlavancagem jornadaDeAlavancagem);
}
