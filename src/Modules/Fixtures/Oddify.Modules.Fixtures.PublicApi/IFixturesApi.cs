using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.PublicApi;

public interface IFixturesApi
{
    Task<Result<LigaResponse>> ObterLigaAsync(Guid ligaId, CancellationToken cancellationToken = default);

    Task<Result<HistoricoDeEquipeResponse>> ObterHistoricoRecenteAsync(
        Guid equipeId,
        int minimoDeJogos,
        CancellationToken cancellationToken = default);

    Task<Result<CotacaoResponse>> ObterCotacaoMaisRecenteAsync(
        Guid partidaId,
        string mercado,
        CancellationToken cancellationToken = default);

    Task<Result<PartidaResponse>> ObterPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default);
}
