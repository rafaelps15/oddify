namespace Oddify.Modules.Users.Domain.Permissions;

public static class WellKnownPermissions
{
    public const string UsersRead = "users:read";

    public const string UsersUpdate = "users:update";

    public const string UsersReadAll = "users:read-all";

    public const string UsersManageRoles = "users:manage-roles";

    public static readonly Guid UsersReadId = Guid.Parse("9a6f9b2a-2e2a-4b8b-9f0a-1a2b3c4d5f01");

    public static readonly Guid UsersUpdateId = Guid.Parse("9a6f9b2a-2e2a-4b8b-9f0a-1a2b3c4d5f02");

    public static readonly Guid UsersReadAllId = Guid.Parse("9a6f9b2a-2e2a-4b8b-9f0a-1a2b3c4d5f03");

    public static readonly Guid UsersManageRolesId = Guid.Parse("9a6f9b2a-2e2a-4b8b-9f0a-1a2b3c4d5f04");
}
