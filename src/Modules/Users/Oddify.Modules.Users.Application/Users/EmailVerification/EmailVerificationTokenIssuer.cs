using System.Security.Cryptography;
using Oddify.Common.Application.Clock;
using Oddify.Modules.Users.Application.Abstractions.Outbox;
using Oddify.Modules.Users.Domain.EmailVerification;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.Modules.Users.Application.Users.EmailVerification;

// Compartilhada por UserRegisteredDomainEventHandler (cadastro) e ResendVerificationCommandHandler
// (reenvio). Não chama SaveChangesAsync — quem invoca decide isso.
// Pública (não internal) — precisa ser registrada por tipo direto em UsersModule.cs
// (Infrastructure), diferente de handlers/validators que são descobertos por reflexão.
public sealed class EmailVerificationTokenIssuer(
    IEmailVerificationTokenRepository tokenRepository,
    IOutboxWriter outboxWriter,
    IDateTimeProvider dateTimeProvider)
{
    private const int TokenSizeInBytes = 32;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    public async Task IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        DateTime agora = dateTimeProvider.UtcNow;

        // Invalida qualquer token não consumido de uma tentativa anterior antes de emitir um
        // novo — token "órfão" nunca entregue é inofensivo de descartar.
        IReadOnlyCollection<EmailVerificationToken> tokensAtivos = await tokenRepository.GetActiveByUserIdAsync(userId, cancellationToken);

        foreach (EmailVerificationToken tokenAtivo in tokensAtivos)
        {
            tokenAtivo.Consume(agora);
        }

        string rawToken = ToBase64Url(RandomNumberGenerator.GetBytes(TokenSizeInBytes));
        DateTime expiraEm = agora.Add(TokenLifetime);

        var token = EmailVerificationToken.Create(userId, rawToken, expiraEm, agora);

        tokenRepository.Insert(token);

        // O e-mail em si só é enviado depois, pelo SendVerificationEmailIntegrationEventConsumer,
        // disparado pelo OutboxProcessorJob — não daqui, que roda dentro do request de cadastro/reenvio.
        outboxWriter.Enqueue(new SendVerificationEmailIntegrationEvent(Guid.NewGuid(), agora, userId, rawToken, expiraEm));
    }

    // Base64Url manual — Microsoft.AspNetCore.WebUtilities não está disponível na Application
    // layer (não referencia pacotes ASP.NET Core), só o essencial de RFC 4648 §5.
    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
