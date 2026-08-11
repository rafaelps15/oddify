using Oddify.Common.Application.Messaging;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.Modules.Apostas.Application.Bancas.CriarBanca;

public sealed record CriarBancaCommand(
    string Nome,
    decimal SaldoInicial,
    decimal PercentualPorEntrada,
    PerfilDeRisco PerfilDeRisco,
    bool ModoPaperTrading) : ICommand<Guid>;
