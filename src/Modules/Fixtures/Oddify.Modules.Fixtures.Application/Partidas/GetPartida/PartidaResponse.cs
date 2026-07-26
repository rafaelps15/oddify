using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetPartida;

public sealed record PartidaResponse(
    Guid Id,
    string IdExterno,
    Guid LigaId,
    Guid EquipeCasaId,
    Guid EquipeVisitanteId,
    DateTime DataUtc,
    SituacaoDaPartida Situacao,
    int? GolsCasa,
    int? GolsVisitante);
