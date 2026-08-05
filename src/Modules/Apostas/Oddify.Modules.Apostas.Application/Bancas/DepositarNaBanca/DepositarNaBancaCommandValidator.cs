using FluentValidation;

namespace Oddify.Modules.Apostas.Application.Bancas.DepositarNaBanca;

internal sealed class DepositarNaBancaCommandValidator : AbstractValidator<DepositarNaBancaCommand>
{
    public DepositarNaBancaCommandValidator()
    {
        RuleFor(c => c.BancaId).NotEmpty();
        RuleFor(c => c.Valor).GreaterThan(0);
    }
}
