using FluentAssertions;
using NSubstitute;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Application.Abstractions.Data;
using Oddify.Modules.Users.Application.Users.RegisterUser;
using Oddify.Modules.Users.Domain.Roles;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.UnitTests.Modules.Users.Users;

public sealed class RegisterUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserRoleRepository _userRoleRepository = Substitute.For<IUserRoleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    private RegisterUserCommandHandler CriarHandler() =>
        new(_userRepository, _userRoleRepository, _unitOfWork, _passwordHasher);

    [Fact]
    public async Task Handle_should_create_user_and_persist_when_email_is_not_registered()
    {
        _userRepository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);
        _userRoleRepository.AnyWithRoleAsync(WellKnownRoles.OwnerId, Arg.Any<CancellationToken>()).Returns(true);
        _passwordHasher.Hash("Password123!").Returns("hashed-password");

        var command = new RegisterUserCommand("user@example.com", "Password123!", "Ada", "Lovelace");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _userRepository.Received(1).Insert(Arg.Is<User>(u => u.PasswordHash == "hashed-password" && u.Email == "user@example.com"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_should_assign_only_registered_role_when_an_owner_already_exists()
    {
        _userRepository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);
        _userRoleRepository.AnyWithRoleAsync(WellKnownRoles.OwnerId, Arg.Any<CancellationToken>()).Returns(true);
        _passwordHasher.Hash("Password123!").Returns("hashed-password");

        var command = new RegisterUserCommand("user@example.com", "Password123!", "Ada", "Lovelace");

        await CriarHandler().Handle(command, CancellationToken.None);

        _userRoleRepository.Received(1).Insert(Arg.Is<UserRole>(ur => ur.RoleId == WellKnownRoles.RegisteredId));
        _userRoleRepository.DidNotReceive().Insert(Arg.Is<UserRole>(ur => ur.RoleId == WellKnownRoles.OwnerId));
    }

    [Fact]
    public async Task Handle_should_assign_owner_and_registered_roles_when_no_owner_exists_yet()
    {
        _userRepository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);
        _userRoleRepository.AnyWithRoleAsync(WellKnownRoles.OwnerId, Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash("Password123!").Returns("hashed-password");

        var command = new RegisterUserCommand("user@example.com", "Password123!", "Ada", "Lovelace");

        await CriarHandler().Handle(command, CancellationToken.None);

        _userRoleRepository.Received(1).Insert(Arg.Is<UserRole>(ur => ur.RoleId == WellKnownRoles.RegisteredId));
        _userRoleRepository.Received(1).Insert(Arg.Is<UserRole>(ur => ur.RoleId == WellKnownRoles.OwnerId));
    }

    [Fact]
    public async Task Handle_should_fail_with_email_already_registered_when_email_is_taken()
    {
        var existente = User.Create("user@example.com", "hashed-password", "Ada", "Lovelace");
        _userRepository.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(existente);

        var command = new RegisterUserCommand("user@example.com", "Password123!", "Ada", "Lovelace");

        Result<Guid> resultado = await CriarHandler().Handle(command, CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UserErrors.EmailAlreadyRegistered);
        _userRepository.DidNotReceive().Insert(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
