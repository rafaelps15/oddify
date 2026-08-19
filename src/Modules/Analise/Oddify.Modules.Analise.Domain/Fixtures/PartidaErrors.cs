using Oddify.Common.Domain;

namespace Oddify.Modules.Analise.Domain.Fixtures;

public static class PartidaErrors
{
    public static Error NotFound(Guid partidaId) =>
        Error.NotFound("Partidas.NotFound", $"A partida espelhada com o identificador {partidaId} não foi encontrada");
}
