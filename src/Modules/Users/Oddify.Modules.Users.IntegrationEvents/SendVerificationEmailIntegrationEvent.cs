using Oddify.Common.Application.EventBus;

namespace Oddify.Modules.Users.IntegrationEvents;

// Carrega o token bruto (não só o hash) — é o único jeito de o consumer conseguir montar o link
// de verificação depois, já que o hash guardado em EmailVerificationToken não é reversível.
public sealed class SendVerificationEmailIntegrationEvent(Guid id, DateTime occurredOnUtc, Guid userId, string rawToken, DateTime expiresAtUtc)
    : IntegrationEvent(id, occurredOnUtc)
{
    public Guid UserId { get; } = userId;

    public string RawToken { get; } = rawToken;

    public DateTime ExpiresAtUtc { get; } = expiresAtUtc;
}
