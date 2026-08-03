using FluentValidation;

namespace Oddify.Modules.Users.Application.Roles.RemoveRoleFromUser;

internal sealed class RemoveRoleFromUserCommandValidator : AbstractValidator<RemoveRoleFromUserCommand>
{
    public RemoveRoleFromUserCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.RoleName).NotEmpty().MaximumLength(100);
    }
}
