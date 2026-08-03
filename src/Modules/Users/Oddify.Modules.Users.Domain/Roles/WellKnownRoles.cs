namespace Oddify.Modules.Users.Domain.Roles;

public static class WellKnownRoles
{
    public const string Registered = "Registered";

    public const string Owner = "Owner";

    public static readonly Guid RegisteredId = Guid.Parse("9a6f9b2a-1e1a-4b8b-9f0a-1a2b3c4d5e01");

    public static readonly Guid OwnerId = Guid.Parse("9a6f9b2a-1e1a-4b8b-9f0a-1a2b3c4d5e02");
}
