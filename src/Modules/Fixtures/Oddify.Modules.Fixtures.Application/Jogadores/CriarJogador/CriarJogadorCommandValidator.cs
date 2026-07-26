using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Jogadores.CriarJogador;

internal sealed class CriarJogadorCommandValidator : AbstractValidator<CriarJogadorCommand>
{
    public CriarJogadorCommandValidator()
    {
        RuleFor(c => c.IdExterno).NotEmpty().MaximumLength(100);
        RuleFor(c => c.EquipeId).NotEmpty();
        RuleFor(c => c.Nome).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Posicao).NotEmpty().MaximumLength(50);
    }
}
