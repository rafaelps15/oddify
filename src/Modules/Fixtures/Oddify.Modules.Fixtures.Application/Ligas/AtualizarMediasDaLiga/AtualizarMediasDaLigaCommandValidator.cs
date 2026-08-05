using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Ligas.AtualizarMediasDaLiga;

internal sealed class AtualizarMediasDaLigaCommandValidator : AbstractValidator<AtualizarMediasDaLigaCommand>
{
    public AtualizarMediasDaLigaCommandValidator()
    {
        RuleFor(c => c.LigaId).NotEmpty().WithMessage("O identificador da liga é obrigatório");
        RuleFor(c => c.MediaDeGols).GreaterThan(0).WithMessage("A média de gols deve ser maior que zero");
        RuleFor(c => c.FatorCasa).GreaterThan(0).WithMessage("O fator casa deve ser maior que zero");
    }
}
