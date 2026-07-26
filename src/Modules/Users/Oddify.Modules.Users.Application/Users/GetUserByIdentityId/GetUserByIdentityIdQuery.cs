using Oddify.Common.Application.Messaging;
using Oddify.Modules.Users.Application.Users.GetUser;

namespace Oddify.Modules.Users.Application.Users.GetUserByIdentityId;

public sealed record GetUserByIdentityIdQuery(string IdentityId) : IQuery<UserResponse>;
