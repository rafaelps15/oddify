using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Users.Application.Users.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : ICommand;
