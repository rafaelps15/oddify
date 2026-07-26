using Oddify.Common.Application.Messaging;
using Oddify.Modules.Users.Application.Users.GetUser;

namespace Oddify.Modules.Users.Application.Users.GetUsers;

public sealed record GetUsersQuery : IQuery<IReadOnlyCollection<UserResponse>>;
