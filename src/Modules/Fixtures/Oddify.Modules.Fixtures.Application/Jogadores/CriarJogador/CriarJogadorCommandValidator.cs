using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Jogadores.CriarJogador;

internal sealed class CriarJogadorCommandValidator : AbstractValidator<CriarJogadorCommand>
{
    public CriarJogadorCommandValidator()
    {
        RuleFor(c => c.IdExterno).NotEmpty().WithMessage("O identificador externo é obrigatório")
            .MaximumLength(100).WithMessage("O identificador externo deve ter no máximo 100 caracteres");
        RuleFor(c => c.EquipeId).NotEmpty().WithMessage("O identificador da equipe é obrigatório");
        RuleFor(c => c.Nome).NotEmpty().WithMessage("O nome do jogador é obrigatório")
            .MaximumLength(200).WithMessage("O nome do jogador deve ter no máximo 200 caracteres");
        RuleFor(c => c.Posicao).NotEmpty().WithMessage("A posição é obrigatória")
            .MaximumLength(50).WithMessage("A posição deve ter no máximo 50 caracteres");
    }
}
