using Oddify.Common.Application.Authorization;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Domain.Roles;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.Modules.Users.Application.Roles.AssignRoleToUser;

internal sealed class AssignRoleToUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUserRoleRepository userRoleRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<AssignRoleToUserCommand>
{
    public async Task<Result> Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        User? user = await userRepository.GetAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(UserErrors.NotFound(request.UserId));
        }

        Role? role = await roleRepository.GetByNameAsync(request.RoleName, cancellationToken);

        if (role is null)
        {
            return Result.Failure(RoleErrors.NotFoundByName(request.RoleName));
        }

        bool alreadyAssigned = await userRoleRepository.ExistsAsync(user.Id, role.Id, cancellationToken);

        if (alreadyAssigned)
        {
            return Result.Failure(RoleErrors.AlreadyAssigned);
        }

        var userRole = UserRole.Create(user.Id, role.Id);

        userRoleRepository.Insert(userRole);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await permissionService.InvalidateAsync(user.Id, cancellationToken);

        return Result.Success();
    }
}
