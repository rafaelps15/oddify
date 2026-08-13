using FluentAssertions;
using Oddify.Modules.Fixtures.Domain.Equipes;

namespace Oddify.UnitTests.Modules.Fixtures.Equipes;

public sealed class EquipeTests
{
    [Fact]
    public void Create_should_raise_EquipeCriadaDomainEvent()
    {
        var equipe = Equipe.Create("equipe-1", "Arsenal", Guid.NewGuid(), logo: null);

        equipe.DomainEvents.Should().ContainSingle(e => e is EquipeCriadaDomainEvent);
    }

    [Fact]
    public void Renomear_should_update_nome_and_raise_event_when_changed()
    {
        var equipe = Equipe.Create("equipe-1", "Arsenal", Guid.NewGuid(), logo: null);
        equipe.ClearDomainEvents();

        equipe.Renomear("Arsenal FC");

        equipe.Nome.Should().Be("Arsenal FC");
        equipe.DomainEvents.Should().ContainSingle(e => e is EquipeRenomeadaDomainEvent);
    }

    [Fact]
    public void Renomear_should_be_a_no_op_when_nome_is_unchanged()
    {
        var equipe = Equipe.Create("equipe-1", "Arsenal", Guid.NewGuid(), logo: null);
        equipe.ClearDomainEvents();

        equipe.Renomear("Arsenal");

        equipe.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AtualizarLogo_should_update_logoUrl_and_raise_event_when_changed()
    {
        var equipe = Equipe.Create("equipe-1", "Arsenal", Guid.NewGuid(), logo: null);
        equipe.ClearDomainEvents();

        equipe.AtualizarLogo("https://media.api-sports.io/football/teams/42.png");

        equipe.Logo.Should().Be("https://media.api-sports.io/football/teams/42.png");
        equipe.DomainEvents.Should().ContainSingle(e => e is EquipeLogoAtualizadoDomainEvent);
    }

    [Fact]
    public void AtualizarLogo_should_be_a_no_op_when_logoUrl_is_unchanged()
    {
        var equipe = Equipe.Create("equipe-1", "Arsenal", Guid.NewGuid(), logo: "https://media.api-sports.io/football/teams/42.png");
        equipe.ClearDomainEvents();

        equipe.AtualizarLogo("https://media.api-sports.io/football/teams/42.png");

        equipe.DomainEvents.Should().BeEmpty();
    }
}
