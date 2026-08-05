using FluentValidation;

namespace Oddify.Modules.Users.Application.Users.VerifyEmail;

internal sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(c => c.Token).NotEmpty().WithMessage("O token é obrigatório");
    }
}
