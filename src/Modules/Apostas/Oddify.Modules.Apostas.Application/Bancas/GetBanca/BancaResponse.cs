using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.Modules.Apostas.Application.Bancas.GetBanca;

public sealed record BancaResponse(
    Guid Id,
    string Nome,
    decimal SaldoAtual,
    decimal PercentualPorEntrada,
    PerfilDeRisco PerfilDeRisco,
    bool ModoPaperTrading);
