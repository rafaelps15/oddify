using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Roles;

public sealed class UserRoleRemovedDomainEvent(Guid userId, Guid roleId) : DomainEvent
{
    public Guid UserId { get; init; } = userId;

    public Guid RoleId { get; init; } = roleId;
}
