using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.Bancas;

public static class BancaErrors
{
    public static Error NotFound(Guid bancaId) =>
        Error.NotFound("Bancas.NotFound", $"A banca com o identificador {bancaId} não foi encontrada");
}
