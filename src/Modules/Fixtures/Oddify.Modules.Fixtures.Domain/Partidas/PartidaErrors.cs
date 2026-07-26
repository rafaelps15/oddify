using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Partidas;

public static class PartidaErrors
{
    public static Error NotFound(Guid partidaId) =>
        Error.NotFound("Partidas.NotFound", $"A partida com o identificador {partidaId} não foi encontrada");

    public static Error JaEncerrada(Guid partidaId) =>
        Error.Problem("Partidas.JaEncerrada", $"A partida com o identificador {partidaId} já está encerrada");

    public static Error AindaNaoEncerrada(Guid partidaId) =>
        Error.Problem("Partidas.AindaNaoEncerrada", $"A partida com o identificador {partidaId} ainda não está encerrada");

    public static readonly Error EquipesIguais = Error.Problem(
        "Partidas.EquipesIguais",
        "A equipe mandante e visitante não podem ser a mesma");
}
