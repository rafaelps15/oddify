using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Users.Application.Users.RequestPasswordReset;

public sealed record RequestPasswordResetCommand(string Email) : ICommand;
