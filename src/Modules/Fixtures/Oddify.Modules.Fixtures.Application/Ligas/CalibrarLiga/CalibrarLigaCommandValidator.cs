using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Ligas.CalibrarLiga;

internal sealed class CalibrarLigaCommandValidator : AbstractValidator<CalibrarLigaCommand>
{
    public CalibrarLigaCommandValidator()
    {
        RuleFor(c => c.LigaId).NotEmpty().WithMessage("O identificador da liga é obrigatório");
    }
}
