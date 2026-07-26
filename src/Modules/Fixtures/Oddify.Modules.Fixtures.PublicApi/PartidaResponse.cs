namespace Oddify.Modules.Fixtures.PublicApi;

public sealed record PartidaResponse(
    Guid Id,
    Guid LigaId,
    Guid EquipeCasaId,
    Guid EquipeVisitanteId,
    DateTime DataUtc,
    string Situacao,
    int? GolsCasa,
    int? GolsVisitante);
