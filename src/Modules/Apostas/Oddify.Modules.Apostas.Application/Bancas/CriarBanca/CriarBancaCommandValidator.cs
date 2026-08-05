using FluentValidation;

namespace Oddify.Modules.Apostas.Application.Bancas.CriarBanca;

internal sealed class CriarBancaCommandValidator : AbstractValidator<CriarBancaCommand>
{
    public CriarBancaCommandValidator()
    {
        RuleFor(c => c.Nome).NotEmpty();
        RuleFor(c => c.SaldoInicial).GreaterThan(0);
        RuleFor(c => c.PercentualPorEntrada).InclusiveBetween(0m, 1m);
    }
}
