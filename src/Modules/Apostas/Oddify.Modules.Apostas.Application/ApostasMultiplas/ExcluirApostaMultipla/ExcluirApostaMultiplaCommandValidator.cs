using FluentValidation;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.ExcluirApostaMultipla;

internal sealed class ExcluirApostaMultiplaCommandValidator : AbstractValidator<ExcluirApostaMultiplaCommand>
{
    public ExcluirApostaMultiplaCommandValidator()
    {
        RuleFor(c => c.ApostaMultiplaId).NotEmpty().WithMessage("O identificador da aposta múltipla é obrigatório");
    }
}
