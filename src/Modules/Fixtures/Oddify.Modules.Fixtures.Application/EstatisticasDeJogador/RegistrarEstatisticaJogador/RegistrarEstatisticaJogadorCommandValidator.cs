using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.EstatisticasDeJogador.RegistrarEstatisticaJogador;

internal sealed class RegistrarEstatisticaJogadorCommandValidator : AbstractValidator<RegistrarEstatisticaJogadorCommand>
{
    public RegistrarEstatisticaJogadorCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty().WithMessage("O identificador da partida é obrigatório");
        RuleFor(c => c.JogadorId).NotEmpty().WithMessage("O identificador do jogador é obrigatório");
        RuleFor(c => c.Gols).GreaterThanOrEqualTo(0).WithMessage("Os gols não podem ser negativos");
        RuleFor(c => c.Assistencias).GreaterThanOrEqualTo(0).WithMessage("As assistências não podem ser negativas");
        RuleFor(c => c.Minutos).InclusiveBetween(0, 120).WithMessage("Os minutos jogados devem estar entre 0 e 120");
        RuleFor(c => c.Nota).InclusiveBetween(0, 10).WithMessage("A nota deve estar entre 0 e 10");
    }
}
