using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Roles;

public sealed class Role : Entity
{
    private Role()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public static Role Create(Guid id, string name)
    {
        var role = new Role
        {
            Id = id,
            Name = name
        };

        return role;
    }
}
