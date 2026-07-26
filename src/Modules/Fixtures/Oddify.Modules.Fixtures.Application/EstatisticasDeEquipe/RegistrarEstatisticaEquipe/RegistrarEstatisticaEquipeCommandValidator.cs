using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.EstatisticasDeEquipe.RegistrarEstatisticaEquipe;

internal sealed class RegistrarEstatisticaEquipeCommandValidator : AbstractValidator<RegistrarEstatisticaEquipeCommand>
{
    public RegistrarEstatisticaEquipeCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty();
        RuleFor(c => c.EquipeId).NotEmpty();
        RuleFor(c => c.Gols).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Finalizacoes).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Escanteios).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Posse).InclusiveBetween(0, 100);
    }
}
