using FluentValidation;

namespace Oddify.Modules.Analise.Application.Fixtures.RegistrarResultadoDaPartida;

internal sealed class RegistrarResultadoDaPartidaCommandValidator : AbstractValidator<RegistrarResultadoDaPartidaCommand>
{
    public RegistrarResultadoDaPartidaCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty();
        RuleFor(c => c.GolsCasa).GreaterThanOrEqualTo(0);
        RuleFor(c => c.GolsVisitante).GreaterThanOrEqualTo(0);
    }
}
