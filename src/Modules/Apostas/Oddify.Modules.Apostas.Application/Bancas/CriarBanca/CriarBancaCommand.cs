using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.Bancas.CriarBanca;

public sealed record CriarBancaCommand(decimal SaldoInicial, bool ModoPaperTrading) : ICommand<Guid>;
