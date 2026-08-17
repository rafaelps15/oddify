using Oddify.Common.Application.EventBus;

namespace Oddify.Modules.Users.IntegrationEvents;

// Carrega o token bruto (não só o hash) — mesmo motivo de SendVerificationEmailIntegrationEvent:
// é o único jeito do consumer montar o link de redefinição, já que o hash não é reversível.
public sealed class SendPasswordResetEmailIntegrationEvent(Guid id, DateTime occurredOnUtc, Guid userId, string rawToken, DateTime expiresAtUtc)
    : IntegrationEvent(id, occurredOnUtc)
{
    public Guid UserId { get; } = userId;

    public string RawToken { get; } = rawToken;

    public DateTime ExpiresAtUtc { get; } = expiresAtUtc;
}
