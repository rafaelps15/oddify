using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Ligas.CriarLiga;

internal sealed class CriarLigaCommandValidator : AbstractValidator<CriarLigaCommand>
{
    public CriarLigaCommandValidator()
    {
        RuleFor(c => c.IdExterno).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(200);
        RuleFor(c => c.MediaDeGols).GreaterThan(0);
        RuleFor(c => c.FatorCasa).GreaterThan(0);
    }
}
