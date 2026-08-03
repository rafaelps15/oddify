using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Users.Application.Roles.GetUserRoles;

public sealed record GetUserRolesQuery(Guid UserId) : IQuery<IReadOnlyCollection<RoleResponse>>;
