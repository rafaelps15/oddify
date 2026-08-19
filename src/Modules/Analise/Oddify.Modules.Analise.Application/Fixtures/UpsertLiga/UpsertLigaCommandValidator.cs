using FluentValidation;

namespace Oddify.Modules.Analise.Application.Fixtures.UpsertLiga;

internal sealed class UpsertLigaCommandValidator : AbstractValidator<UpsertLigaCommand>
{
    public UpsertLigaCommandValidator()
    {
        RuleFor(c => c.LigaId).NotEmpty();
        RuleFor(c => c.Nome).NotEmpty();
        RuleFor(c => c.MediaDeGols).GreaterThan(0);
        RuleFor(c => c.FatorCasa).GreaterThan(0);
    }
}
