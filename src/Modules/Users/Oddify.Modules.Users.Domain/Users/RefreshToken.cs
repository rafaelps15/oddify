using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Users;

public sealed class RefreshToken : Entity
{
    private RefreshToken(Guid id, Guid userId, string token, DateTime expiresAtUtc)
    {
        Id = id;
        UserId = userId;
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Token { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAtUtc) =>
        new(Guid.NewGuid(), userId, token, expiresAtUtc);

    public void Rotate(string newToken, DateTime newExpiresAtUtc)
    {
        Token = newToken;
        ExpiresAtUtc = newExpiresAtUtc;
    }
}
