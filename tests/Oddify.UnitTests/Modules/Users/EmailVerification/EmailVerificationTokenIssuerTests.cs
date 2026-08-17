using FluentAssertions;
using NSubstitute;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Outbox;
using Oddify.Modules.Users.Application.Users.EmailVerification;
using Oddify.Modules.Users.Domain.EmailVerification;
using Oddify.Modules.Users.IntegrationEvents;

namespace Oddify.UnitTests.Modules.Users.EmailVerification;

public sealed class EmailVerificationTokenIssuerTests
{
    private readonly IEmailVerificationTokenRepository _tokenRepository = Substitute.For<IEmailVerificationTokenRepository>();
    private readonly IOutboxWriter _outboxWriter = Substitute.For<IOutboxWriter>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private EmailVerificationTokenIssuer CriarIssuer() => new(_tokenRepository, _outboxWriter, _dateTimeProvider);

    [Fact]
    public async Task IssueAsync_should_insert_a_new_token_and_enqueue_the_verification_email()
    {
        var userId = Guid.NewGuid();
        _tokenRepository.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<EmailVerificationToken>)[]);
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        await CriarIssuer().IssueAsync(userId, CancellationToken.None);

        _tokenRepository.Received(1).Insert(Arg.Is<EmailVerificationToken>(t => t.UserId == userId));
        _outboxWriter.Received(1).Enqueue(Arg.Is<SendVerificationEmailIntegrationEvent>(e => e.UserId == userId));
    }

    [Fact]
    public async Task IssueAsync_should_invalidate_previous_unconsumed_tokens_before_creating_a_new_one()
    {
        var userId = Guid.NewGuid();
        DateTime agora = DateTime.UtcNow;
        var tokenOrfao = EmailVerificationToken.Create(userId, "old-raw-token", agora.AddHours(24), agora.AddMinutes(-5));
        _tokenRepository.GetActiveByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyCollection<EmailVerificationToken>)[tokenOrfao]);
        _dateTimeProvider.UtcNow.Returns(agora);

        await CriarIssuer().IssueAsync(userId, CancellationToken.None);

        tokenOrfao.ConsumedAtUtc.Should().Be(agora);
        _tokenRepository.Received(1).Insert(Arg.Is<EmailVerificationToken>(t => t.UserId == userId));
    }
}
