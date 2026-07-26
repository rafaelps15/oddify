using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Partidas.Reagendar;

internal sealed class ReagendarCommandValidator : AbstractValidator<ReagendarCommand>
{
    public ReagendarCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty();
        RuleFor(c => c.NovaDataUtc).NotEmpty();
    }
}
