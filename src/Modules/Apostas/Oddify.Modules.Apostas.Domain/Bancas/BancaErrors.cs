using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.Bancas;

public static class BancaErrors
{
    public static Error NotFound(Guid bancaId) =>
        Error.NotFound("Bancas.NotFound", $"A banca com o identificador {bancaId} não foi encontrada");

    public static readonly Error OddInvalida =
        Error.Problem("Bancas.OddInvalida", "A odd precisa ser maior que 1 para calcular uma sugestão de stake.");
}
