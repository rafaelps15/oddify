using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.Users;

public sealed class RefreshToken : Entity
{
    // Fonte única de "por quanto tempo um refresh token vale" — LoginCommandHandler e
    // RefreshAccessTokenCommandHandler usam essa constante pra computar ExpiresAtUtc na hora de
    // criar/rotacionar; a entidade recebe a data já calculada (não DateTimeProvider, que é
    // abstração de Application) pra continuar fácil de testar com datas arbitrárias.
    public const int DefaultExpirationDays = 7;

    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Token { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    // Nullable — clientes que não mandam o header User-Agent (raro, mas possível fora de
    // navegador) não têm essa informação disponível pra capturar no login/refresh.
    public string? UserAgent { get; private set; }

    // Cada linha desta tabela já É uma "sessão" (um login = um refresh token, nunca reaproveitado
    // entre logins — ver LoginCommandHandler) — LastSeenAtUtc é o que diferencia "sessão parada
    // há 3 dias" de "sessão usada agora há pouco" na tela de Sessões, atualizado a cada rotação.
    public DateTime LastSeenAtUtc { get; private set; }

    // Sem domain event em Create/Rotate — bookkeeping de sessão local a este módulo, sem nenhum
    // outro módulo ou consumidor plausível precisando saber que uma sessão foi aberta/renovada
    // (diferente de EmailVerificationToken/PasswordResetToken, cujo Create dispara envio de e-mail
    // via outbox — aqui não há efeito colateral nenhum a coordenar).
    public static RefreshToken Create(Guid userId, string token, DateTime expiresAtUtc, DateTime createdAtUtc, string? userAgent)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = createdAtUtc,
            UserAgent = userAgent,
            LastSeenAtUtc = createdAtUtc
        };

        return refreshToken;
    }

    public void Rotate(string newToken, DateTime newExpiresAtUtc, DateTime rotatedAtUtc)
    {
        Token = newToken;
        ExpiresAtUtc = newExpiresAtUtc;
        LastSeenAtUtc = rotatedAtUtc;
    }
}
