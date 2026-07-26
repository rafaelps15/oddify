using FluentValidation;

namespace Oddify.Modules.Fixtures.Application.Jogadores.TransferirJogador;

internal sealed class TransferirJogadorCommandValidator : AbstractValidator<TransferirJogadorCommand>
{
    public TransferirJogadorCommandValidator()
    {
        RuleFor(c => c.JogadorId).NotEmpty();
        RuleFor(c => c.NovaEquipeId).NotEmpty();
    }
}
