using FluentValidation;

namespace Oddify.Modules.Users.Application.Users.ResetPassword;

internal sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(c => c.Token).NotEmpty().WithMessage("O token é obrigatório");
        RuleFor(c => c.NewPassword)
            .NotEmpty().WithMessage("A senha é obrigatória")
            .MinimumLength(6).WithMessage("A senha deve ter no mínimo 6 caracteres")
            .MaximumLength(100).WithMessage("A senha deve ter no máximo 100 caracteres")
            .Must(password => password.Any(char.IsDigit)).WithMessage("A senha deve conter pelo menos um número")
            .Must(password => password.Any(c => !char.IsLetterOrDigit(c))).WithMessage("A senha deve conter pelo menos um caractere especial");
    }
}
