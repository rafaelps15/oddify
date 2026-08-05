namespace Oddify.Modules.Apostas.Application.Bancas.GetResumoDaBanca;

public sealed record ResumoDaBancaResponse(
    Guid BancaId,
    decimal SaldoAtual,
    decimal TotalDepositado,
    decimal TotalGanho,
    decimal TotalPerdido,
    int QuantidadeDeApostas);
