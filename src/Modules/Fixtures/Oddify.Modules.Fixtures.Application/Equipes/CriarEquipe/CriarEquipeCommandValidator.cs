using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Equipes.CriarEquipe;

internal sealed class CriarEquipeCommandValidator : AbstractValidator<CriarEquipeCommand>
{
    public CriarEquipeCommandValidator()
    {
        RuleFor(c => c.IdExterno).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(200);
        RuleFor(c => c.LigaId).NotEmpty();
    }
}
