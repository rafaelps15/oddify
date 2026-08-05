using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Ligas.CriarLiga;

internal sealed class CriarLigaCommandValidator : AbstractValidator<CriarLigaCommand>
{
    public CriarLigaCommandValidator()
    {
        RuleFor(c => c.IdExterno).NotEmpty().WithMessage("O identificador externo é obrigatório")
            .MaximumLength(100).WithMessage("O identificador externo deve ter no máximo 100 caracteres");
        RuleFor(c => c.Nome).NotEmpty().WithMessage("O nome da liga é obrigatório")
            .MaximumLength(200).WithMessage("O nome da liga deve ter no máximo 200 caracteres");
        RuleFor(c => c.MediaDeGols).GreaterThan(0).WithMessage("A média de gols deve ser maior que zero");
        RuleFor(c => c.FatorCasa).GreaterThan(0).WithMessage("O fator casa deve ser maior que zero");
    }
}
