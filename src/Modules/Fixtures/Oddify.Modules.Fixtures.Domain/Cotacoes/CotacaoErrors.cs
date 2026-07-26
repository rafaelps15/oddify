using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Domain.Cotacoes;

public static class CotacaoErrors
{
    public static Error NotFound(Guid cotacaoId) =>
        Error.NotFound("Cotacoes.NotFound", $"A cotação com o identificador {cotacaoId} não foi encontrada");

    public static readonly Error OddInvalida = Error.Problem(
        "Cotacoes.OddInvalida",
        "A odd informada deve ser maior que 1.0");

    public static Error NenhumaCotacaoEncontrada(Guid partidaId, string mercado) =>
        Error.NotFound(
            "Cotacoes.NenhumaCotacaoEncontrada",
            $"Nenhuma cotação foi encontrada para a partida {partidaId} no mercado {mercado}");
}
