using FluentValidation;

namespace Oddify.Modules.Users.Application.Roles.AssignRoleToUser;

internal sealed class AssignRoleToUserCommandValidator : AbstractValidator<AssignRoleToUserCommand>
{
    public AssignRoleToUserCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.RoleName).NotEmpty().MaximumLength(100);
    }
}
