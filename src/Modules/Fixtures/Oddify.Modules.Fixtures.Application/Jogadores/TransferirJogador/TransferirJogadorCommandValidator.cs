using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Jogadores.TransferirJogador;

internal sealed class TransferirJogadorCommandValidator : AbstractValidator<TransferirJogadorCommand>
{
    public TransferirJogadorCommandValidator()
    {
        RuleFor(c => c.JogadorId).NotEmpty().WithMessage("O identificador do jogador é obrigatório");
        RuleFor(c => c.NovaEquipeId).NotEmpty().WithMessage("O identificador da nova equipe é obrigatório");
    }
}
