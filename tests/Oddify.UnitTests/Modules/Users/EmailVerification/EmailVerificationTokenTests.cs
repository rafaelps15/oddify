using FluentAssertions;
using Oddify.Modules.Users.Domain.EmailVerification;

namespace Oddify.UnitTests.Modules.Users.EmailVerification;

public sealed class EmailVerificationTokenTests
{
    [Fact]
    public void Create_should_set_properties_and_not_raise_any_domain_event()
    {
        var userId = Guid.NewGuid();
        DateTime agora = DateTime.UtcNow;
        DateTime expiraEm = agora.AddHours(24);

        var token = EmailVerificationToken.Create(userId, "raw-token-value", expiraEm, agora);

        token.UserId.Should().Be(userId);
        token.TokenHash.Should().Be(EmailVerificationToken.Hash("raw-token-value"));
        token.TokenHash.Should().NotBe("raw-token-value");
        token.ExpiresAtUtc.Should().Be(expiraEm);
        token.CreatedAtUtc.Should().Be(agora);
        token.ConsumedAtUtc.Should().BeNull();

        // Quem precisa mandar o e-mail de verificação faz isso via IOutboxWriter, chamado por
        // quem invoca Create (ver EmailVerificationTokenIssuer) — não é responsabilidade da
        // entidade levantar um domain event pra isso.
        token.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Hash_should_be_deterministic()
    {
        EmailVerificationToken.Hash("same-token").Should().Be(EmailVerificationToken.Hash("same-token"));
    }

    [Fact]
    public void Consume_should_set_ConsumedAtUtc()
    {
        var token = EmailVerificationToken.Create(Guid.NewGuid(), "raw-token", DateTime.UtcNow.AddHours(24), DateTime.UtcNow);
        DateTime consumidoEm = DateTime.UtcNow;

        token.Consume(consumidoEm);

        token.ConsumedAtUtc.Should().Be(consumidoEm);
    }

    [Fact]
    public void Consume_should_be_idempotent()
    {
        var token = EmailVerificationToken.Create(Guid.NewGuid(), "raw-token", DateTime.UtcNow.AddHours(24), DateTime.UtcNow);
        DateTime primeiroConsumo = DateTime.UtcNow;
        token.Consume(primeiroConsumo);

        token.Consume(DateTime.UtcNow.AddMinutes(5));

        token.ConsumedAtUtc.Should().Be(primeiroConsumo);
    }
}
