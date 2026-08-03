namespace Oddify.Modules.Users.Domain.Roles;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
