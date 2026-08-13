using FluentValidation;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.EstornarLiquidacaoMultipla;

internal sealed class EstornarLiquidacaoMultiplaCommandValidator : AbstractValidator<EstornarLiquidacaoMultiplaCommand>
{
    public EstornarLiquidacaoMultiplaCommandValidator()
    {
        RuleFor(c => c.ApostaMultiplaId).NotEmpty().WithMessage("O identificador da aposta múltipla é obrigatório");
    }
}
