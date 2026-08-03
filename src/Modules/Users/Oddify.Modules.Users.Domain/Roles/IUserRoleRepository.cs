namespace Oddify.Modules.Users.Domain.Roles;

public interface IUserRoleRepository
{
    Task<UserRole?> GetAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    Task<bool> AnyWithRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    void Insert(UserRole userRole);

    void Remove(UserRole userRole);
}
