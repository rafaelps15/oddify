using FluentValidation;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.IniciarJornada;

internal sealed class IniciarJornadaCommandValidator : AbstractValidator<IniciarJornadaCommand>
{
    public IniciarJornadaCommandValidator()
    {
        RuleFor(c => c.FaixaMeta).IsInEnum();
        RuleFor(c => c.ValorInicial).GreaterThan(0);
    }
}
