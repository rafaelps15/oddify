using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.EscalacoesDeJogador.RegistrarEscalacaoJogador;

internal sealed class RegistrarEscalacaoJogadorCommandValidator : AbstractValidator<RegistrarEscalacaoJogadorCommand>
{
    public RegistrarEscalacaoJogadorCommandValidator()
    {
        RuleFor(c => c.EscalacaoId).NotEmpty().WithMessage("O identificador da escalação é obrigatório");
        RuleFor(c => c.JogadorId).NotEmpty().WithMessage("O identificador do jogador é obrigatório");
        RuleFor(c => c.Posicao).NotEmpty().MaximumLength(20).WithMessage("A posição é obrigatória");
        RuleFor(c => c.Numero).GreaterThan(0).When(c => c.Numero.HasValue).WithMessage("O número da camisa deve ser positivo");
    }
}
