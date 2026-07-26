using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Ligas.AtualizarMediasDaLiga;

internal sealed class AtualizarMediasDaLigaCommandValidator : AbstractValidator<AtualizarMediasDaLigaCommand>
{
    public AtualizarMediasDaLigaCommandValidator()
    {
        RuleFor(c => c.LigaId).NotEmpty();
        RuleFor(c => c.MediaDeGols).GreaterThan(0);
        RuleFor(c => c.FatorCasa).GreaterThan(0);
    }
}
