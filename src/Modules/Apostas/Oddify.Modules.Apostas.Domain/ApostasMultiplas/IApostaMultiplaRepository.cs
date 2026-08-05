namespace Oddify.Modules.Apostas.Domain.ApostasMultiplas;

public interface IApostaMultiplaRepository
{
    Task<ApostaMultipla?> GetAsync(Guid id, Guid usuarioId, CancellationToken cancellationToken = default);

    void Insert(ApostaMultipla apostaMultipla);
}
