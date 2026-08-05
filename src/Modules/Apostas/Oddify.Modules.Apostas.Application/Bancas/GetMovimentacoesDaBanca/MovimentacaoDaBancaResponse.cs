using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;

namespace Oddify.Modules.Apostas.Application.Bancas.GetMovimentacoesDaBanca;

public sealed record MovimentacaoDaBancaResponse(
    Guid Id,
    TipoDeMovimentacao Tipo,
    decimal Valor,
    decimal SaldoAposMovimentacao,
    Guid? ApostaMultiplaId,
    DateTime CriadaEmUtc);
