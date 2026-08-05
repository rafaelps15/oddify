using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.EstatisticasDeEquipe.RegistrarEstatisticaEquipe;

internal sealed class RegistrarEstatisticaEquipeCommandValidator : AbstractValidator<RegistrarEstatisticaEquipeCommand>
{
    public RegistrarEstatisticaEquipeCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty().WithMessage("O identificador da partida é obrigatório");
        RuleFor(c => c.EquipeId).NotEmpty().WithMessage("O identificador da equipe é obrigatório");
        RuleFor(c => c.Gols).GreaterThanOrEqualTo(0).WithMessage("Os gols não podem ser negativos");
        RuleFor(c => c.Finalizacoes).GreaterThanOrEqualTo(0).WithMessage("As finalizações não podem ser negativas");
        RuleFor(c => c.Escanteios).GreaterThanOrEqualTo(0).WithMessage("Os escanteios não podem ser negativos");
        RuleFor(c => c.Posse).InclusiveBetween(0, 100).WithMessage("A posse de bola deve estar entre 0 e 100");
    }
}
