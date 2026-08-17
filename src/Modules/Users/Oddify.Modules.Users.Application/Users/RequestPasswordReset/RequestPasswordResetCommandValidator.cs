using FluentValidation;

namespace Oddify.Modules.Users.Application.Users.RequestPasswordReset;

internal sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório")
            .EmailAddress().WithMessage("O e-mail informado é inválido")
            .MaximumLength(300).WithMessage("O e-mail deve ter no máximo 300 caracteres");
    }
}
