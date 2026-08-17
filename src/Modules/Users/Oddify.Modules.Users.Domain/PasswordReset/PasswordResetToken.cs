using System.Security.Cryptography;
using System.Text;
using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.PasswordReset;

public sealed class PasswordResetToken : Entity
{
    private PasswordResetToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ConsumedAtUtc { get; private set; }

    // Mesmo desenho de EmailVerificationToken: sem domain event (entrega do e-mail exige
    // durabilidade, não uma reação in-process — ver PasswordResetTokenIssuer/IOutboxWriter).
    public static PasswordResetToken Create(Guid userId, string rawToken, DateTime expiresAtUtc, DateTime createdAtUtc)
    {
        return new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = createdAtUtc
        };
    }

    // Mesmo motivo de EmailVerificationToken.Hash: token já é alta entropia (256 bits
    // aleatórios), hash simples e rápido é suficiente, não precisa de PBKDF2/bcrypt.
    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    // Idempotente de propósito — mesmo motivo de EmailVerificationToken.Consume.
    public void Consume(DateTime consumedAtUtc)
    {
        if (ConsumedAtUtc is not null)
        {
            return;
        }

        ConsumedAtUtc = consumedAtUtc;
    }
}
