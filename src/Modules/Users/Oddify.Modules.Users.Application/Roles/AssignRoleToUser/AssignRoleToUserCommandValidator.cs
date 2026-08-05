using FluentValidation;

namespace Oddify.Modules.Users.Application.Roles.AssignRoleToUser;

internal sealed class AssignRoleToUserCommandValidator : AbstractValidator<AssignRoleToUserCommand>
{
    public AssignRoleToUserCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty().WithMessage("O identificador do usuário é obrigatório");
        RuleFor(c => c.RoleName).NotEmpty().WithMessage("O nome do papel é obrigatório")
            .MaximumLength(100).WithMessage("O nome do papel deve ter no máximo 100 caracteres");
    }
}
