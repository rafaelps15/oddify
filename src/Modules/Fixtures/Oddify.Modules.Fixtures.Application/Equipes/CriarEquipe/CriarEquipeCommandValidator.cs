using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Equipes.CriarEquipe;

internal sealed class CriarEquipeCommandValidator : AbstractValidator<CriarEquipeCommand>
{
    public CriarEquipeCommandValidator()
    {
        RuleFor(c => c.IdExterno).NotEmpty().WithMessage("O identificador externo é obrigatório")
            .MaximumLength(100).WithMessage("O identificador externo deve ter no máximo 100 caracteres");
        RuleFor(c => c.Nome).NotEmpty().WithMessage("O nome da equipe é obrigatório")
            .MaximumLength(200).WithMessage("O nome da equipe deve ter no máximo 200 caracteres");
        RuleFor(c => c.LigaId).NotEmpty().WithMessage("O identificador da liga é obrigatório");
    }
}
