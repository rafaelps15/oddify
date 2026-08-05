namespace Oddify.Modules.Apostas.Application.Bancas.GetMovimentacoesDaBanca;

public sealed record GetMovimentacoesDaBancaResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyCollection<MovimentacaoDaBancaResponse> Items);
