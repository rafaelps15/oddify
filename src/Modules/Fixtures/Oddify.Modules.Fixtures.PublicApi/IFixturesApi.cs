namespace Oddify.Modules.Fixtures.PublicApi;

public interface IFixturesApi
{
    Task<LigaResponse?> ObterLigaAsync(Guid ligaId, CancellationToken cancellationToken = default);

    Task<HistoricoDeEquipeResponse?> ObterHistoricoRecenteAsync(
        Guid equipeId,
        int minimoDeJogos,
        CancellationToken cancellationToken = default);

    Task<CotacaoResponse?> ObterCotacaoMaisRecenteAsync(
        Guid partidaId,
        string mercado,
        CancellationToken cancellationToken = default);

    Task<PartidaResponse?> ObterPartidaAsync(Guid partidaId, CancellationToken cancellationToken = default);
}
