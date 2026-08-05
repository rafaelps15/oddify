using FluentValidation;

namespace Oddify.Modules.Apostas.Application.Bancas.DepositarNaBanca;

internal sealed class DepositarNaBancaCommandValidator : AbstractValidator<DepositarNaBancaCommand>
{
    public DepositarNaBancaCommandValidator()
    {
        RuleFor(c => c.BancaId).NotEmpty().WithMessage("O identificador da banca é obrigatório");
        RuleFor(c => c.Valor).GreaterThan(0).WithMessage("O valor do depósito deve ser maior que zero");
    }
}
