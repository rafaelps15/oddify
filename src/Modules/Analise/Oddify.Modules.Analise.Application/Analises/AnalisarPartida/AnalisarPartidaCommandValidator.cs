using FluentValidation;

namespace Oddify.Modules.Analise.Application.Analises.AnalisarPartida;

internal sealed class AnalisarPartidaCommandValidator : AbstractValidator<AnalisarPartidaCommand>
{
    public AnalisarPartidaCommandValidator()
    {
        RuleFor(c => c.PartidaId).NotEmpty();
        RuleFor(c => c.Mercado).NotEmpty().MaximumLength(100);
    }
}
