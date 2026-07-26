using FluentAssertions;
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
}
