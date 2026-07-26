using FluentValidation;

namespace Oddify.Modules.Apostas.Application.Bancas.CriarBanca;

internal sealed class CriarBancaCommandValidator : AbstractValidator<CriarBancaCommand>
{
    public CriarBancaCommandValidator()
    {
        RuleFor(c => c.SaldoInicial).GreaterThan(0);
    }
}
