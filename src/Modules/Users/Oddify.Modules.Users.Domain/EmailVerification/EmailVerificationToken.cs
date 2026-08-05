using System.Security.Cryptography;
using System.Text;
using Oddify.Common.Domain;

namespace Oddify.Modules.Users.Domain.EmailVerification;

public sealed class EmailVerificationToken : Entity
{
    private EmailVerificationToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ConsumedAtUtc { get; private set; }

    // Não levanta domain event — quem precisa mandar o e-mail de verificação (que exige entrega
    // garantida, não uma reação in-process) é responsabilidade de quem chama Create, via
    // IOutboxWriter (ver EmailVerificationTokenIssuer). Um domain event aqui só faria sentido
    // pra reações síncronas locais, que não é o caso.
    public static EmailVerificationToken Create(Guid userId, string rawToken, DateTime expiresAtUtc, DateTime createdAtUtc)
    {
        return new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(rawToken),
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = createdAtUtc
        };
    }

    // Único ponto que calcula o hash — usado tanto no Create (pra gravar) quanto por quem
    // recebe o token de volta (VerifyEmailCommandHandler) pra montar a chave de busca. Um hash
    // simples e rápido (não PBKDF2 como senha) é o certo aqui: o token já é alta entropia
    // (256 bits aleatórios), não uma senha escolhida por humano sujeita a força bruta por
    // dicionário.
    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    // Idempotente de propósito — VerifyEmailCommandHandler pode acabar sendo chamado mais de
    // uma vez pro mesmo token (double-click, retry de consumer); a segunda chamada é um no-op.
    public void Consume(DateTime consumedAtUtc)
    {
        if (ConsumedAtUtc is not null)
        {
            return;
        }

        ConsumedAtUtc = consumedAtUtc;
    }
}
