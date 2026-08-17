using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Users;

public sealed class User : Entity
{
    private User()
    {
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public bool IsEmailVerified { get; private set; }

    public DateTime? EmailVerifiedAtUtc { get; private set; }

    public static User Create(string email, string passwordHash, string firstName, string lastName)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            IsEmailVerified = false
        };

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

    // Sem checagem de no-op (comparar hashes não diz se a senha "mudou" de verdade — o mesmo
    // texto em claro gera hashes diferentes a cada chamada por causa do salt do hasher) — sempre
    // aplica e sempre levanta o evento.
    public void SetPassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;

        Raise(new PasswordResetDomainEvent(Id));
    }

    public Result MarkEmailAsVerified(DateTime verifiedAtUtc)
    {
        if (IsEmailVerified)
        {
            return Result.Failure(UserErrors.EmailAlreadyVerified);
        }

        IsEmailVerified = true;
        EmailVerifiedAtUtc = verifiedAtUtc;

        Raise(new EmailVerifiedDomainEvent(Id));

        return Result.Success();
    }
}
