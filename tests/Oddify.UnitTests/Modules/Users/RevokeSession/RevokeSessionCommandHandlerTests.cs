using FluentAssertions;
using NSubstitute;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Application.Users.RevokeSession;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.UnitTests.Modules.Users.RevokeSession;

public sealed class RevokeSessionCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();

    private RevokeSessionCommandHandler CriarHandler() => new(_refreshTokenRepository, _unitOfWork, _userContext);

    [Fact]
    public async Task Handle_should_fail_when_session_not_found()
    {
        var sessionId = Guid.NewGuid();
        _refreshTokenRepository.GetAsync(sessionId, Arg.Any<CancellationToken>()).Returns((RefreshToken?)null);

        Result resultado = await CriarHandler().Handle(new RevokeSessionCommand(sessionId), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UserErrors.SessionNotFound);
    }

    [Fact]
    public async Task Handle_should_fail_when_session_belongs_to_another_user()
    {
        DateTime agora = DateTime.UtcNow;
        var session = RefreshToken.Create(Guid.NewGuid(), "rt-1", agora.AddDays(7), agora, "Mozilla/5.0");
        _userContext.UserId.Returns(Guid.NewGuid());
        _refreshTokenRepository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        Result resultado = await CriarHandler().Handle(new RevokeSessionCommand(session.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UserErrors.SessionNotFound);
        _refreshTokenRepository.DidNotReceive().Delete(Arg.Any<RefreshToken>());
    }

    [Fact]
    public async Task Handle_should_delete_session_and_save_when_owned_by_current_user()
    {
        DateTime agora = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var session = RefreshToken.Create(userId, "rt-1", agora.AddDays(7), agora, "Mozilla/5.0");
        _userContext.UserId.Returns(userId);
        _refreshTokenRepository.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        Result resultado = await CriarHandler().Handle(new RevokeSessionCommand(session.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _refreshTokenRepository.Received(1).Delete(session);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
