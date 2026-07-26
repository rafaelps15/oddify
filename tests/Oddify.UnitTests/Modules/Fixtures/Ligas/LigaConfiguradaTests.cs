using FluentAssertions;
using Oddify.Modules.Fixtures.Domain.Ligas;

namespace Oddify.UnitTests.Modules.Fixtures.Ligas;

public sealed class LigaConfiguradaTests
{
    [Fact]
    public void Create_should_raise_LigaConfiguradaCriadaDomainEvent_and_start_uncalibrated()
    {
        var liga = LigaConfigurada.Create("liga-1", "Premier League", 2.5m, 1.1m);

        liga.DomainEvents.Should().ContainSingle(e => e is LigaConfiguradaCriadaDomainEvent);
        liga.Calibrada.Should().BeFalse();
    }

    [Fact]
    public void AtualizarMedias_should_update_values_and_raise_event_when_changed()
    {
        var liga = LigaConfigurada.Create("liga-1", "Premier League", 2.5m, 1.1m);
        liga.ClearDomainEvents();

        liga.AtualizarMedias(2.8m, 1.2m);

        liga.MediaDeGols.Should().Be(2.8m);
        liga.FatorCasa.Should().Be(1.2m);
        liga.DomainEvents.Should().ContainSingle(e => e is LigaMediasAtualizadasDomainEvent);
    }

    [Fact]
    public void AtualizarMedias_should_not_raise_event_when_values_are_unchanged()
    {
        var liga = LigaConfigurada.Create("liga-1", "Premier League", 2.5m, 1.1m);
        liga.ClearDomainEvents();

        liga.AtualizarMedias(2.5m, 1.1m);

        liga.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Calibrar_should_set_calibrada_true_and_raise_event()
    {
        var liga = LigaConfigurada.Create("liga-1", "Premier League", 2.5m, 1.1m);
        liga.ClearDomainEvents();

        liga.Calibrar();

        liga.Calibrada.Should().BeTrue();
        liga.DomainEvents.Should().ContainSingle(e => e is LigaCalibradaAlteradaDomainEvent);
    }

    [Fact]
    public void Calibrar_should_be_a_no_op_when_already_calibrada()
    {
        var liga = LigaConfigurada.Create("liga-1", "Premier League", 2.5m, 1.1m);
        liga.Calibrar();
        liga.ClearDomainEvents();

        liga.Calibrar();

        liga.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Descalibrar_should_set_calibrada_false_and_raise_event()
    {
        var liga = LigaConfigurada.Create("liga-1", "Premier League", 2.5m, 1.1m);
        liga.Calibrar();
        liga.ClearDomainEvents();

        liga.Descalibrar();

        liga.Calibrada.Should().BeFalse();
        liga.DomainEvents.Should().ContainSingle(e => e is LigaCalibradaAlteradaDomainEvent);
    }

    [Fact]
    public void Descalibrar_should_be_a_no_op_when_already_descalibrada()
    {
        var liga = LigaConfigurada.Create("liga-1", "Premier League", 2.5m, 1.1m);
        liga.ClearDomainEvents();

        liga.Descalibrar();

        liga.DomainEvents.Should().BeEmpty();
    }
}
