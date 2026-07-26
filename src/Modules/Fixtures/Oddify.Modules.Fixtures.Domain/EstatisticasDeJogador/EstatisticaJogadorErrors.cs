using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.EstatisticasDeJogador;

public static class EstatisticaJogadorErrors
{
    public static Error NotFound(Guid estatisticaJogadorId) =>
        Error.NotFound(
            "EstatisticasDeJogador.NotFound",
            $"A estatística de jogador com o identificador {estatisticaJogadorId} não foi encontrada");
}
