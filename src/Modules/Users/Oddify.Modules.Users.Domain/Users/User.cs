using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Users;

public sealed class User : Entity
{
    private User(Guid id, string identityId, string email, string firstName, string lastName)
    {
        Id = id;
        IdentityId = identityId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }

    public Guid Id { get; private set; }

    public string IdentityId { get; private set; }

    public string Email { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public static User Create(string identityId, string email, string firstName, string lastName)
    {
        var user = new User(Guid.NewGuid(), identityId, email, firstName, lastName);

        user.Raise(new UserRegisteredDomainEvent(user.Id));

        return user;
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        if (FirstName == firstName && LastName == lastName)
        {
            return;
        }

        FirstName = firstName;
        LastName = lastName;

        Raise(new UserProfileUpdatedDomainEvent(Id, firstName, lastName));
    }
}
