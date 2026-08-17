namespace Oddify.Modules.Users.Application.Users.GetSessions;

// UserAgent nunca vem null daqui mesmo sendo opcional no domínio (RefreshToken.UserAgent) — a
// query cobre esse caso com COALESCE, porque o front (entities/session.ts) trata o campo como
// sempre presente.
public sealed record SessionResponse(Guid Id, string UserAgent, DateTime CreatedAtUtc, DateTime LastSeenAtUtc);
