using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Jogadores;

public static class JogadorErrors
{
    public static Error NotFound(Guid jogadorId) =>
        Error.NotFound("Jogadores.NotFound", $"O jogador com o identificador {jogadorId} não foi encontrado");

    public static readonly Error JaNaEquipe = Error.Problem(
        "Jogadores.JaNaEquipe",
        "O jogador já pertence a essa equipe");
}
