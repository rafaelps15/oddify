using FluentAssertions;
using NSubstitute;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Application.Users.RegisterUser;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.UnitTests.Modules.Users.Users;

public sealed class RegisterUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RegisterUserCommandHandler CriarHandler() => new(_userRepository, _unitOfWork);

    [Fact]
    public async Task Handle_should_create_user_and_persist_when_identityId_is_not_registered()
    {
        _userRepository.GetByIdentityIdAsync("identity-1", Arg.Any<CancellationToken>()).Returns((User?)null);

        var command = new RegisterUserCommand("identity-1", "user@example.com", "Ada", "Lovelace");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _userRepository.Received(1).Insert(Arg.Is<User>(u => u.IdentityId == "identity-1" && u.Email == "user@example.com"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_fail_when_identityId_is_already_registered()
    {
        var existente = User.Create("identity-1", "user@example.com", "Ada", "Lovelace");
        _userRepository.GetByIdentityIdAsync("identity-1", Arg.Any<CancellationToken>()).Returns(existente);

        var command = new RegisterUserCommand("identity-1", "outro@example.com", "Grace", "Hopper");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UserErrors.IdentityIdAlreadyRegistered);
        _userRepository.DidNotReceive().Insert(Arg.Any<User>());
    }
}
