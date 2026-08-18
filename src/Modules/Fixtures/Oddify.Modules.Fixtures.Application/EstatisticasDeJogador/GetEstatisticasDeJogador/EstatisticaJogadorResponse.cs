namespace Oddify.Modules.Fixtures.Application.EstatisticasDeJogador.GetEstatisticasDeJogador;

public sealed record EstatisticaJogadorResponse(
    Guid Id,
    Guid PartidaId,
    Guid JogadorId,
    int Gols,
    int Assistencias,
    int Minutos,
    bool Titular,
    decimal Nota);
