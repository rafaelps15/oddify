using FluentValidation;

namespace Oddify.Modules.Apostas.Application.Bancas.CriarBanca;

internal sealed class CriarBancaCommandValidator : AbstractValidator<CriarBancaCommand>
{
    public CriarBancaCommandValidator()
    {
        RuleFor(c => c.Nome).NotEmpty().WithMessage("O nome da banca é obrigatório");
        RuleFor(c => c.SaldoInicial).GreaterThan(0).WithMessage("O saldo inicial deve ser maior que zero");
        RuleFor(c => c.PercentualPorEntrada).InclusiveBetween(0m, 1m).WithMessage("O percentual por entrada deve estar entre 0 e 1");
    }
}
