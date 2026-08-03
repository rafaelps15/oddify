using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Users.Application.Roles.AssignRoleToUser;

public sealed record AssignRoleToUserCommand(Guid UserId, string RoleName) : ICommand;
