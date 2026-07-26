using FluentValidation;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;

internal sealed class LiquidarMultiplaCommandValidator : AbstractValidator<LiquidarMultiplaCommand>
{
    public LiquidarMultiplaCommandValidator()
    {
        RuleFor(c => c.ApostaMultiplaId).NotEmpty();
    }
}
