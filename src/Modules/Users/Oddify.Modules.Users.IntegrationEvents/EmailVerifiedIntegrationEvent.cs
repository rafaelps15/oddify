using Oddify.Common.Application.EventBus;

namespace Oddify.Modules.Users.IntegrationEvents;

public sealed class EmailVerifiedIntegrationEvent(Guid id, DateTime occurredOnUtc, Guid userId)
    : IntegrationEvent(id, occurredOnUtc)
{
    public Guid UserId { get; } = userId;
}
