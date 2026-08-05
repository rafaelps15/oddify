using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Users.Application.Users.VerifyEmail;

public sealed record VerifyEmailCommand(string Token) : ICommand;
