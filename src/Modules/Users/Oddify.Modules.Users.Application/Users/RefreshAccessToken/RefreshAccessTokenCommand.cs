using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Users.Application.Users.RefreshAccessToken;

public sealed record RefreshAccessTokenCommand(string RefreshToken) : ICommand<AccessTokensResponse>;
