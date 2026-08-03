using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Users.Application.Roles.RemoveRoleFromUser;

public sealed record RemoveRoleFromUserCommand(Guid UserId, string RoleName) : ICommand;
