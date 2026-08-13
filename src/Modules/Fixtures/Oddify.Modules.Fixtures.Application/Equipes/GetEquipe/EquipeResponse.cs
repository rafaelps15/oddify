namespace Oddify.Modules.Fixtures.Application.Equipes.GetEquipe;

public sealed record EquipeResponse(Guid Id, string IdExterno, string Nome, Guid LigaId, string? Logo);
