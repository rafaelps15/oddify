using FluentValidation;

namespace Oddify.Modules.Users.Application.Users.RegisterUser;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório")
            .EmailAddress().WithMessage("O e-mail informado é inválido")
            .MaximumLength(300).WithMessage("O e-mail deve ter no máximo 300 caracteres")
            .Must(email => !email.Any(char.IsUpper))
            .WithMessage("O e-mail não pode conter letras maiúsculas");
        RuleFor(c => c.Password)
            .NotEmpty().WithMessage("A senha é obrigatória")
            .MinimumLength(6).WithMessage("A senha deve ter no mínimo 6 caracteres")
            .MaximumLength(100).WithMessage("A senha deve ter no máximo 100 caracteres")
            .Must(password => password.Any(char.IsDigit)).WithMessage("A senha deve conter pelo menos um número")
            .Must(password => password.Any(c => !char.IsLetterOrDigit(c))).WithMessage("A senha deve conter pelo menos um caractere especial");
        RuleFor(c => c.FirstName).NotEmpty().WithMessage("O nome é obrigatório")
            .MaximumLength(200).WithMessage("O nome deve ter no máximo 200 caracteres");
        RuleFor(c => c.LastName).NotEmpty().WithMessage("O sobrenome é obrigatório")
            .MaximumLength(200).WithMessage("O sobrenome deve ter no máximo 200 caracteres");
    }
}
