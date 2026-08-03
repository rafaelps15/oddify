using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Roles;

public static class RoleErrors
{
    public static Error NotFound(Guid roleId) =>
        Error.NotFound("Roles.NotFound", $"The role with the identifier {roleId} not found");

    public static Error NotFoundByName(string name) =>
        Error.NotFound("Roles.NotFoundByName", $"The role with the name {name} not found");

    public static readonly Error AlreadyAssigned = Error.Conflict(
        "Roles.AlreadyAssigned",
        "The user already has this role assigned");

    public static readonly Error NotAssigned = Error.Problem(
        "Roles.NotAssigned",
        "The user does not have this role assigned");
}
