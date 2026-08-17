using FluentValidation;

namespace Oddify.Modules.Users.Application.Users.RevokeSession;

internal sealed class RevokeSessionCommandValidator : AbstractValidator<RevokeSessionCommand>
{
    public RevokeSessionCommandValidator()
    {
        RuleFor(c => c.SessionId).NotEmpty().WithMessage("O identificador da sessão é obrigatório");
    }
}
