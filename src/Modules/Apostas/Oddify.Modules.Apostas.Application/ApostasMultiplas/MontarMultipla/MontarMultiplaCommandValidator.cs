using FluentValidation;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.MontarMultipla;

internal sealed class MontarMultiplaCommandValidator : AbstractValidator<MontarMultiplaCommand>
{
    public MontarMultiplaCommandValidator()
    {
        RuleFor(c => c.BancaId).NotEmpty();
        RuleFor(c => c.AnaliseIds).Must(ids => ids.Count is 2 or 3).WithMessage("A múltipla deve ter 2 ou 3 pernas");
        RuleForEach(c => c.AnaliseIds).NotEmpty();
    }
}
