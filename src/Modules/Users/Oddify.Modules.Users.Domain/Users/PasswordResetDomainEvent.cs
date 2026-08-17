using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Users;

public sealed class PasswordResetDomainEvent(Guid userId) : DomainEvent
{
    public Guid UserId { get; } = userId;
}
