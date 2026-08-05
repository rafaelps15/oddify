using FluentValidation;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.MontarMultipla;

internal sealed class MontarMultiplaCommandValidator : AbstractValidator<MontarMultiplaCommand>
{
    public MontarMultiplaCommandValidator()
    {
        RuleFor(c => c.BancaId).NotEmpty().WithMessage("O identificador da banca é obrigatório");
        RuleFor(c => c.AnaliseIds).Must(ids => ids.Count is 2 or 3).WithMessage("A múltipla deve ter 2 ou 3 pernas");
        RuleForEach(c => c.AnaliseIds).NotEmpty().WithMessage("O identificador da análise é obrigatório");
        RuleFor(c => c.Descricao).MaximumLength(500).WithMessage("A descrição deve ter no máximo 500 caracteres");
    }
}
