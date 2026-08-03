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
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };
    }
}
