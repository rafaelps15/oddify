using FluentValidation;

namespace Oddify.Modules.Users.Application.Roles.RemoveRoleFromUser;

internal sealed class RemoveRoleFromUserCommandValidator : AbstractValidator<RemoveRoleFromUserCommand>
{
    public RemoveRoleFromUserCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty().WithMessage("O identificador do usuário é obrigatório");
        RuleFor(c => c.RoleName).NotEmpty().WithMessage("O nome do papel é obrigatório")
            .MaximumLength(100).WithMessage("O nome do papel deve ter no máximo 100 caracteres");
    }
}
