namespace Oddify.Modules.Fixtures.Application.EstatisticasDeEquipe.GetEstatisticasDeEquipe;

public sealed record EstatisticaEquipeResponse(
    Guid Id,
    Guid PartidaId,
    Guid EquipeId,
    int Gols,
    int Finalizacoes,
    int Escanteios,
    decimal Posse);
