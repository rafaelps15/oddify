using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Users;

public sealed class EmailVerifiedDomainEvent(Guid userId) : DomainEvent
{
    public Guid UserId { get; } = userId;
}
