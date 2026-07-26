using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Users;

public sealed class User : Entity
{
    private User(Guid id, string email, string passwordHash, string firstName, string lastName)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public static User Create(string email, string passwordHash, string firstName, string lastName)
    {
        var user = new User(Guid.NewGuid(), email, passwordHash, firstName, lastName);

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
