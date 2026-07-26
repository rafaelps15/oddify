using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Equipes.RenomearEquipe;

internal sealed class RenomearEquipeCommandValidator : AbstractValidator<RenomearEquipeCommand>
{
    public RenomearEquipeCommandValidator()
    {
        RuleFor(c => c.EquipeId).NotEmpty();
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(200);
    }
}
