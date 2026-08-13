using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

public static class JornadaDeAlavancagemErrors
{
    public static Error NotFound(Guid jornadaDeAlavancagemId) =>
        Error.NotFound("JornadasDeAlavancagem.NotFound", $"A jornada de alavancagem com o identificador {jornadaDeAlavancagemId} não foi encontrada");

    public static Error NaoEstaEmAndamento(Guid jornadaDeAlavancagemId) =>
        Error.Problem("JornadasDeAlavancagem.NaoEstaEmAndamento", $"A jornada de alavancagem com o identificador {jornadaDeAlavancagemId} não está em andamento");

    public static readonly Error ValorInicialAbaixoDoMinimo = Error.Problem(
        "JornadasDeAlavancagem.ValorInicialAbaixoDoMinimo",
        "O valor inicial é menor que a banca mínima exigida pela faixa de meta escolhida");

    public static Error JaTemJornadaEmAndamento(Guid usuarioId) =>
        Error.Conflict("JornadasDeAlavancagem.JaTemJornadaEmAndamento", $"O usuário {usuarioId} já tem uma jornada de alavancagem em andamento");
}
