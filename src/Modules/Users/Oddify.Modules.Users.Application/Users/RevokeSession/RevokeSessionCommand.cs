using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Users.Application.Users.RevokeSession;

public sealed record RevokeSessionCommand(Guid SessionId) : ICommand;
