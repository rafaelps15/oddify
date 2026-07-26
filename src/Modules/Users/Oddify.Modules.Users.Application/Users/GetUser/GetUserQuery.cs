using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Users.Application.Users.GetUser;

public sealed record GetUserQuery(Guid UserId) : IQuery<UserResponse>;
