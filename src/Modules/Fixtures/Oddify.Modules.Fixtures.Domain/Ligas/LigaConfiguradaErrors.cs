using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Ligas;

public static class LigaConfiguradaErrors
{
    public static Error NotFound(Guid ligaId) =>
        Error.NotFound("Ligas.NotFound", $"A liga com o identificador {ligaId} não foi encontrada");

    public static readonly Error IdExternoJaCadastrado = Error.Conflict(
        "Ligas.IdExternoJaCadastrado",
        "Já existe uma liga cadastrada com esse identificador externo");

    public static Error SincronizacaoConcorrente(Guid ligaId) => Error.Conflict(
        "Ligas.SincronizacaoConcorrente",
        $"A sincronização de fixtures da liga {ligaId} colidiu com outra execução concorrente; tente novamente.");
}
