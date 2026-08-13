using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.PassosDaJornada;

public static class PassoDaJornadaErrors
{
    public static Error NotFound(Guid passoDaJornadaId) =>
        Error.NotFound("PassosDaJornada.NotFound", $"O passo de jornada com o identificador {passoDaJornadaId} não foi encontrado");

    public static Error NaoEstaMontado(Guid passoDaJornadaId) =>
        Error.Problem("PassosDaJornada.NaoEstaMontado", $"O passo de jornada com o identificador {passoDaJornadaId} não está montado");

    public static Error NaoEstaEmAberto(Guid passoDaJornadaId) =>
        Error.Problem("PassosDaJornada.NaoEstaEmAberto", $"O passo de jornada com o identificador {passoDaJornadaId} não está em aberto");

    public static readonly Error SemOportunidadesSuficientes = Error.Problem(
        "PassosDaJornada.SemOportunidadesSuficientes",
        "Não há oportunidades válidas suficientes (odds <= 1.50, edge real, jogos distintos) para montar o passo hoje");
}
