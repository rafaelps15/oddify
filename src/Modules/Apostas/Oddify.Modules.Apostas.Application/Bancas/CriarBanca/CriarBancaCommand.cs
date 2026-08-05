using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.Bancas.CriarBanca;

public sealed record CriarBancaCommand(
    string Nome,
    decimal SaldoInicial,
    decimal PercentualPorEntrada,
    bool ModoPaperTrading) : ICommand<Guid>;
