using FluentAssertions;
using Oddify.Common.Domain;
using Oddify.Modules.Users.Domain.Users;

namespace Oddify.UnitTests.Modules.Users.Users;

public sealed class UserTests
{
    [Fact]
    public void Create_should_raise_UserRegisteredDomainEvent()
    {
        var user = User.Create("identity-1", "user@example.com", "Ada", "Lovelace");

        user.DomainEvents.Should().ContainSingle(e => e is UserRegisteredDomainEvent);
    }

    [Fact]
    public void UpdateProfile_should_update_values_and_raise_event_when_changed()
    {
        var user = User.Create("identity-1", "user@example.com", "Ada", "Lovelace");
        user.ClearDomainEvents();

        user.UpdateProfile("Grace", "Hopper");

        user.FirstName.Should().Be("Grace");
        user.LastName.Should().Be("Hopper");
        user.DomainEvents.Should().ContainSingle(e => e is UserProfileUpdatedDomainEvent);
    }

    [Fact]
    public void UpdateProfile_should_not_raise_event_when_values_are_unchanged()
    {
        var user = User.Create("identity-1", "user@example.com", "Ada", "Lovelace");
        user.ClearDomainEvents();

        user.UpdateProfile("Ada", "Lovelace");

        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Create_should_start_with_email_not_verified()
    {
        var user = User.Create("identity-1", "user@example.com", "Ada", "Lovelace");

        user.IsEmailVerified.Should().BeFalse();
        user.EmailVerifiedAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkEmailAsVerified_should_set_verified_and_raise_event()
    {
        var user = User.Create("identity-1", "user@example.com", "Ada", "Lovelace");
        user.ClearDomainEvents();
        DateTime agora = DateTime.UtcNow;

        Result resultado = user.MarkEmailAsVerified(agora);

        resultado.IsSuccess.Should().BeTrue();
        user.IsEmailVerified.Should().BeTrue();
        user.EmailVerifiedAtUtc.Should().Be(agora);
        user.DomainEvents.Should().ContainSingle(e => e is EmailVerifiedDomainEvent);
    }

    [Fact]
    public void MarkEmailAsVerified_should_fail_when_already_verified()
    {
        var user = User.Create("identity-1", "user@example.com", "Ada", "Lovelace");
        user.MarkEmailAsVerified(DateTime.UtcNow);
        user.ClearDomainEvents();

        Result resultado = user.MarkEmailAsVerified(DateTime.UtcNow);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(UserErrors.EmailAlreadyVerified);
        user.DomainEvents.Should().BeEmpty();
    }
}
