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

    // Nunca deveria acontecer de verdade — IniciarJornadaCommandValidator já garante FaixaMeta.IsInEnum()
    // e o seed de FaixaDeMetaCatalogo cobre os três valores do enum. Só cai aqui se o seed for apagado
    // manualmente ou um novo valor for adicionado ao enum sem a migration correspondente.
    public static Error FaixaDeMetaCatalogoNaoEncontrado(FaixaDeMeta faixaMeta) =>
        Error.Failure("JornadasDeAlavancagem.FaixaDeMetaCatalogoNaoEncontrado", $"O catálogo da faixa de meta {faixaMeta} não foi encontrado");
}
