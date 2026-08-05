using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Equipes.RenomearEquipe;

internal sealed class RenomearEquipeCommandValidator : AbstractValidator<RenomearEquipeCommand>
{
    public RenomearEquipeCommandValidator()
    {
        RuleFor(c => c.EquipeId).NotEmpty().WithMessage("O identificador da equipe é obrigatório");
        RuleFor(c => c.Nome).NotEmpty().WithMessage("O nome da equipe é obrigatório")
            .MaximumLength(200).WithMessage("O nome da equipe deve ter no máximo 200 caracteres");
    }
}
