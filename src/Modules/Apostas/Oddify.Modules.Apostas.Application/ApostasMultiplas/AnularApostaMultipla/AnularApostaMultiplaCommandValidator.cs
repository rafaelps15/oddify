using FluentValidation;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.AnularApostaMultipla;

internal sealed class AnularApostaMultiplaCommandValidator : AbstractValidator<AnularApostaMultiplaCommand>
{
    public AnularApostaMultiplaCommandValidator()
    {
        RuleFor(c => c.ApostaMultiplaId).NotEmpty().WithMessage("O identificador da aposta múltipla é obrigatório");
    }
}
