using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;

public sealed record FixtureExternoDto(
    string IdExterno,
    string EquipeCasaIdExterno,
    string NomeEquipeCasa,
    string? EquipeCasaLogo,
    string EquipeVisitanteIdExterno,
    string NomeEquipeVisitante,
    string? EquipeVisitanteLogo,
    DateTime DataUtc,
    bool Encerrada,
    int? GolsCasa,
    int? GolsVisitante,
    // Extraído de league.round (ex.: "Regular Season - 4") — a api-football não expõe um número
    // de rodada isolado, só esse rótulo textual; quando o formato não termina em número (fases de
    // playoff/grupo com outro rótulo), fica 0 (ver ApiFootballClient.ExtrairRodada).
    int Rodada,
    string? LigaFlag);

// Recorte mínimo do fixture pra sincronização ao vivo — não carrega times/liga/rodada (a partida
// já existe no banco, só times/liga já resolvidos importam aqui é o par id-externo + placar
// atual). `EmAndamento`/`Encerrada` são mutuamente exclusivos; nenhum dos dois marcado significa
// um status que a sincronização ao vivo ignora (ex.: adiada, cancelada — ver ApiFootballClient).
public sealed record FixtureAoVivoExternoDto(string IdExterno, bool EmAndamento, bool Encerrada, int? GolsCasa, int? GolsVisitante);

public interface IApiFootballClient
{
    Task<Result<IReadOnlyCollection<FixtureExternoDto>>> GetFixturesAsync(string ligaIdExterno, int temporada, CancellationToken cancellationToken = default);

    // `live=<id>-<id>-...` da API-Football — só as partidas em andamento agora dentre as ligas
    // informadas (não existe filtro por temporada aqui: "ao vivo" é sempre o jogo de agora).
    Task<Result<IReadOnlyCollection<FixtureAoVivoExternoDto>>> GetFixturesAoVivoAsync(IReadOnlyCollection<string> ligaIdsExternos, CancellationToken cancellationToken = default);

    Task<bool> VerificarStatusAsync(CancellationToken cancellationToken = default);
}
