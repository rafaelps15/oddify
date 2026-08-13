namespace Oddify.Modules.Apostas.Domain.ApostasMultiplas;

public interface IApostaMultiplaRepository
{
    Task<ApostaMultipla?> GetAsync(Guid id, Guid usuarioId, CancellationToken cancellationToken = default);

    // Sem escopo de usuário — só pro domain-event handler que reage a ApostaMultiplaLiquidadaDomainEvent
    // (não tem um usuário autenticado direto na requisição nesse ponto, só o id da aposta que o
    // próprio evento carrega).
    Task<ApostaMultipla?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Usado por AvaliarPassoDaJornadaCommandHandler (e o domain-event handler que o dispara) pra
    // conferir se as N apostas de um passo já resolveram e contar green/red — não filtra por
    // usuarioId porque PassoDaJornadaId já é um identificador interno confiável nesse ponto (a
    // jornada dona do passo já foi carregada de forma escopada antes).
    Task<IReadOnlyCollection<ApostaMultipla>> GetPorPassoDaJornadaAsync(Guid passoDaJornadaId, CancellationToken cancellationToken = default);

    void Insert(ApostaMultipla apostaMultipla);

    void Delete(ApostaMultipla apostaMultipla);
}
