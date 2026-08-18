using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.EscalacoesDeJogador;

public static class EscalacaoJogadorErrors
{
    public static Error NotFound(Guid escalacaoJogadorId) =>
        Error.NotFound(
            "EscalacoesDeJogador.NotFound",
            $"A escalação de jogador com o identificador {escalacaoJogadorId} não foi encontrada");
}
