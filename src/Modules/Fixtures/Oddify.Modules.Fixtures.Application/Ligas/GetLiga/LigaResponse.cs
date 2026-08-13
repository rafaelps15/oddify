namespace Oddify.Modules.Fixtures.Application.Ligas.GetLiga;

public sealed record LigaResponse(
    Guid Id,
    string IdExterno,
    string Nome,
    decimal MediaDeGols,
    decimal FatorCasa,
    bool Calibrada,
    string? Bandeira);
