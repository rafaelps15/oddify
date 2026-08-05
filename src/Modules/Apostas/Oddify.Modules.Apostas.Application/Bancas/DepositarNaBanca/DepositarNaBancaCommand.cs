using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.Bancas.DepositarNaBanca;

public sealed record DepositarNaBancaCommand(Guid BancaId, decimal Valor) : ICommand;
