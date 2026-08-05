using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Partidas.RegistrarResultado;

internal sealed class RegistrarResultadoCommandValidator : AbstractValidator<RegistrarResultadoCommand>
{
    public RegistrarResultadoCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty().WithMessage("O identificador da partida é obrigatório");
        RuleFor(c => c.GolsCasa).GreaterThanOrEqualTo(0).WithMessage("Os gols da equipe mandante não podem ser negativos");
        RuleFor(c => c.GolsVisitante).GreaterThanOrEqualTo(0).WithMessage("Os gols da equipe visitante não podem ser negativos");
    }
}
