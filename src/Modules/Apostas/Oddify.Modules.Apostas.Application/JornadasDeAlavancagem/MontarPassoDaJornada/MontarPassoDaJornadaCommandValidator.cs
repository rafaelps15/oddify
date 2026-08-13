using FluentValidation;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.MontarPassoDaJornada;

internal sealed class MontarPassoDaJornadaCommandValidator : AbstractValidator<MontarPassoDaJornadaCommand>
{
    public MontarPassoDaJornadaCommandValidator()
    {
        RuleFor(c => c.JornadaId).NotEmpty().WithMessage("O identificador da jornada é obrigatório");
    }
}
