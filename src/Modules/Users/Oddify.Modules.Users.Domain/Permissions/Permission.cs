using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Permissions;

public sealed class Permission : Entity
{
    private Permission()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public static Permission Create(Guid id, string name)
    {
        return new Permission
        {
            Id = id,
            Name = name
        };
    }
}
