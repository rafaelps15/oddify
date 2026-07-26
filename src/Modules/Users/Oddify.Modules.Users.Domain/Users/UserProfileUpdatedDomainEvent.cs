using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Users;

public sealed class UserProfileUpdatedDomainEvent(Guid userId, string firstName, string lastName) : DomainEvent
{
    public Guid UserId { get; } = userId;

    public string FirstName { get; } = firstName;

    public string LastName { get; } = lastName;
}
