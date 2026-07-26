namespace Oddify.Modules.Apostas.Application.Bancas.GetBanca;

public sealed record BancaResponse(Guid Id, decimal SaldoAtual, bool ModoPaperTrading);
