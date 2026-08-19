using Oddify.Common.Application.Authorization;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Domain.Roles;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.Modules.Users.Application.Roles.RemoveRoleFromUser;

internal sealed class RemoveRoleFromUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUserRoleRepository userRoleRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<RemoveRoleFromUserCommand>
{
    public async Task<Result> Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
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

        UserRole? userRole = await userRoleRepository.GetAsync(user.Id, role.Id, cancellationToken);

        if (userRole is null)
        {
            return Result.Failure(RoleErrors.NotAssigned);
        }

        userRole.MarkAsRemoved();

        userRoleRepository.Remove(userRole);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Domain events deste projeto só são despachados de forma assíncrona (outbox + job, ver
        // CLAUDE.md §5/§10) — a invalidação do cache de permissões continua chamada direto aqui,
        // não move pra um IDomainEventHandler<UserRoleRemovedDomainEvent>, porque isso introduziria
        // uma janela real (o intervalo de polling do job) em que um papel já revogado ainda seria
        // servido do cache. O evento existe pra outros consumidores (auditoria, notificação) que
        // podem tolerar esse atraso; a invalidação em si é segurança, não pode.
        await permissionService.InvalidateAsync(user.Id, cancellationToken);

        return Result.Success();
    }
}
