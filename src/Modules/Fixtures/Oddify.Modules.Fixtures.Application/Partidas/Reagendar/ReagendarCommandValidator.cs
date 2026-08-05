using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Partidas.Reagendar;

internal sealed class ReagendarCommandValidator : AbstractValidator<ReagendarCommand>
{
    public ReagendarCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty().WithMessage("O identificador da partida é obrigatório");
        RuleFor(c => c.NovaDataUtc).NotEmpty().WithMessage("A nova data é obrigatória");
    }
}
