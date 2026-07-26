using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.EstatisticasDeJogador.RegistrarEstatisticaJogador;

internal sealed class RegistrarEstatisticaJogadorCommandValidator : AbstractValidator<RegistrarEstatisticaJogadorCommand>
{
    public RegistrarEstatisticaJogadorCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty();
        RuleFor(c => c.JogadorId).NotEmpty();
        RuleFor(c => c.Gols).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Assistencias).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Minutos).InclusiveBetween(0, 120);
        RuleFor(c => c.Nota).InclusiveBetween(0, 10);
    }
}
