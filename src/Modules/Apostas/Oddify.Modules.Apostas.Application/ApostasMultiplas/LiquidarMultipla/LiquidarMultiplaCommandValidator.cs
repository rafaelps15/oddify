using FluentValidation;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.LiquidarMultipla;

internal sealed class LiquidarMultiplaCommandValidator : AbstractValidator<LiquidarMultiplaCommand>
{
    public LiquidarMultiplaCommandValidator()
    {
        RuleFor(c => c.ApostaMultiplaId).NotEmpty().WithMessage("O identificador da aposta múltipla é obrigatório");
        RuleFor(c => c.UsuarioId).NotEmpty().WithMessage("O identificador do usuário é obrigatório");
    }
}
