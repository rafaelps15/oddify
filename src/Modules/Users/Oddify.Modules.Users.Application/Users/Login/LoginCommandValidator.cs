using FluentValidation;

namespace Oddify.Modules.Users.Application.Users.Login;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório")
            .EmailAddress().WithMessage("O e-mail informado é inválido")
            .Must(email => !email.Any(char.IsUpper))
            .WithMessage("O e-mail não pode conter letras maiúsculas");
        RuleFor(c => c.Password).NotEmpty().WithMessage("A senha é obrigatória");
    }
}
