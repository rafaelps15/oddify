using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Application.Users.UpdateUserProfile;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.UnitTests.Modules.Users.Users;

public sealed class UpdateUserProfileCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UpdateUserProfileCommandHandler CriarHandler() => new(_userRepository, _unitOfWork);

    [Fact]
    public async Task Handle_should_update_profile_and_persist_when_user_exists()
    {
        var user = User.Create("identity-1", "user@example.com", "Ada", "Lovelace");
        _userRepository.GetAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var command = new UpdateUserProfileCommand(user.Id, "Grace", "Hopper");

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        user.FirstName.Should().Be("Grace");
        user.LastName.Should().Be("Hopper");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_user_not_found()
    {
        var userId = Guid.NewGuid();
        _userRepository.GetAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var command = new UpdateUserProfileCommand(userId, "Grace", "Hopper");

        Result resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UserErrors.NotFound(userId));
    }
}
