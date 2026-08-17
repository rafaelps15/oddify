using System.Security.Cryptography;
using Oddify.Common.Application.Clock;
using Oddify.Modules.Users.Application.Abstractions.Outbox;
using Oddify.Modules.Users.Domain.PasswordReset;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.Modules.Users.Application.Users.PasswordReset;

// Mesmo desenho de EmailVerificationTokenIssuer — pública (não internal) porque é registrada por
// tipo direto em UsersModule.cs, chamada só por RequestPasswordResetCommandHandler. Não chama
// SaveChangesAsync — quem invoca decide isso.
public sealed class PasswordResetTokenIssuer(
    IPasswordResetTokenRepository tokenRepository,
    IOutboxWriter outboxWriter,
    IDateTimeProvider dateTimeProvider)
{
    private const int TokenSizeInBytes = 32;

    // Mais curto que EmailVerificationTokenIssuer (24h) de propósito — um link de redefinição de
    // senha interceptado é mais sensível que um link de confirmação de e-mail, então a janela de
    // validade é bem menor (padrão de mercado: 1h).
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        DateTime agora = dateTimeProvider.UtcNow;

        // Invalida qualquer pedido de redefinição anterior ainda não consumido — mesmo motivo de
        // EmailVerificationTokenIssuer: token órfão nunca entregue é inofensivo de descartar.
        IReadOnlyCollection<PasswordResetToken> tokensAtivos = await tokenRepository.GetActiveByUserIdAsync(userId, cancellationToken);

        foreach (PasswordResetToken tokenAtivo in tokensAtivos)
        {
            tokenAtivo.Consume(agora);
        }

        string rawToken = ToBase64Url(RandomNumberGenerator.GetBytes(TokenSizeInBytes));
        DateTime expiraEm = agora.Add(TokenLifetime);

        var token = PasswordResetToken.Create(userId, rawToken, expiraEm, agora);

        tokenRepository.Insert(token);

        outboxWriter.Enqueue(new SendPasswordResetEmailIntegrationEvent(Guid.NewGuid(), agora, userId, rawToken, expiraEm));
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
