namespace Oddify.Modules.Fixtures.Application.Partidas.GetPartidasResumo;

public sealed record PartidaResumoResponse(
    Guid Id,
    Guid LigaId,
    string LigaNome,
    DateTime DataUtc,
    Guid EquipeCasaId,
    string EquipeCasaNome,
    string? EquipeCasaLogo,
    Guid EquipeVisitanteId,
    string EquipeVisitanteNome,
    string? EquipeVisitanteLogo);
