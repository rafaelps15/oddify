using FluentValidation;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.AvaliarPassoDaJornada;

internal sealed class AvaliarPassoDaJornadaCommandValidator : AbstractValidator<AvaliarPassoDaJornadaCommand>
{
    public AvaliarPassoDaJornadaCommandValidator()
    {
        RuleFor(c => c.PassoId).NotEmpty().WithMessage("O identificador do passo é obrigatório");
    }
}
