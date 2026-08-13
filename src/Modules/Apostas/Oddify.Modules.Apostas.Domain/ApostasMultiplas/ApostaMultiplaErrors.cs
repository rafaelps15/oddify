using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Domain.ApostasMultiplas;

public static class ApostaMultiplaErrors
{
    public static Error NotFound(Guid apostaMultiplaId) =>
        Error.NotFound("ApostasMultiplas.NotFound", $"A aposta múltipla com o identificador {apostaMultiplaId} não foi encontrada");

    public static Error JaLiquidada(Guid apostaMultiplaId) =>
        Error.Problem("ApostasMultiplas.JaLiquidada", $"A aposta múltipla com o identificador {apostaMultiplaId} já foi liquidada");

    public static Error AindaNaoLiquidada(Guid apostaMultiplaId) =>
        Error.Problem("ApostasMultiplas.AindaNaoLiquidada", $"A aposta múltipla com o identificador {apostaMultiplaId} ainda não foi liquidada");

    public static readonly Error PartidasRepetidas = Error.Problem(
        "ApostasMultiplas.PartidasRepetidas",
        "A múltipla não pode conter mais de uma perna para a mesma partida");

    public static readonly Error StakeNulo = Error.Problem(
        "ApostasMultiplas.StakeNulo",
        "O stake calculado via Kelly foi zero ou negativo; a aposta não deve ser feita");
}
