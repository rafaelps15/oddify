using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Users.Application.Users.Login;

public sealed record LoginCommand(string Email, string Password, string? UserAgent) : ICommand<AccessTokensResponse>;
