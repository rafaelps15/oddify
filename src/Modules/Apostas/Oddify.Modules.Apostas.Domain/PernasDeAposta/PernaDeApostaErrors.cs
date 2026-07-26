using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.PernasDeAposta;

public static class PernaDeApostaErrors
{
    public static Error NotFound(Guid pernaDeApostaId) =>
        Error.NotFound("PernasDeAposta.NotFound", $"A perna de aposta com o identificador {pernaDeApostaId} não foi encontrada");

    public static Error JaResolvida(Guid pernaDeApostaId) =>
        Error.Problem("PernasDeAposta.JaResolvida", $"A perna de aposta com o identificador {pernaDeApostaId} já foi resolvida");
}
