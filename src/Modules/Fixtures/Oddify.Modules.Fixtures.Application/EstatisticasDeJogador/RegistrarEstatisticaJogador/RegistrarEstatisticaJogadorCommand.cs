using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.EstatisticasDeJogador.RegistrarEstatisticaJogador;

public sealed record RegistrarEstatisticaJogadorCommand(
    Guid PartidaId,
    Guid JogadorId,
    int Gols,
    int Assistencias,
    int Minutos,
    bool Titular,
    decimal Nota) : ICommand<Guid>;
