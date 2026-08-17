namespace Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

public interface IFaixaDeMetaCatalogoRepository
{
    Task<FaixaDeMetaCatalogo?> GetAsync(FaixaDeMeta faixa, CancellationToken cancellationToken = default);
}
