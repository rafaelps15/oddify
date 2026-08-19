using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Roles;

public sealed class UserRole : Entity
{
    private UserRole()
    {
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public static UserRole Create(Guid userId, Guid roleId)
    {
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };

        userRole.Raise(new UserRoleAssignedDomainEvent(userId, roleId));

        return userRole;
    }

    // Sem transição de estado própria (a linha é fisicamente removida pelo repository, não há
    // "status" pra mutar) — esse método existe só pra dar um lugar de onde levantar o evento antes
    // da remoção, mesmo padrão de uma entidade que não tem outro jeito de anunciar "isto está
    // prestes a deixar de existir". Chame antes de IUserRoleRepository.Remove(...).
    public void MarkAsRemoved()
    {
        Raise(new UserRoleRemovedDomainEvent(UserId, RoleId));
    }
}
