using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Partidas.RegistrarResultado;

internal sealed class RegistrarResultadoCommandValidator : AbstractValidator<RegistrarResultadoCommand>
{
    public RegistrarResultadoCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty();
        RuleFor(c => c.GolsCasa).GreaterThanOrEqualTo(0);
        RuleFor(c => c.GolsVisitante).GreaterThanOrEqualTo(0);
    }
}
