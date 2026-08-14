using FluentValidation;

namespace Oddify.Modules.Apostas.Application.Bancas.CriarBancaInicial;

internal sealed class CriarBancaInicialCommandValidator : AbstractValidator<CriarBancaInicialCommand>
{
    public CriarBancaInicialCommandValidator()
    {
        RuleFor(c => c.UsuarioId).NotEmpty();
    }
}
