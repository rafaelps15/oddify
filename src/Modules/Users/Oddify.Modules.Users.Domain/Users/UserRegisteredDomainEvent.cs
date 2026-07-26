using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Users;

public sealed class UserRegisteredDomainEvent(Guid userId) : DomainEvent
{
    public Guid UserId { get; } = userId;
}
