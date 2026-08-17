using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Users.Application.Users.GetSessions;

public sealed record GetSessionsQuery : IQuery<IReadOnlyCollection<SessionResponse>>;
