using FluentAssertions;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Domain.Partidas;

namespace Oddify.UnitTests.Modules.Fixtures.Partidas;

public sealed class PartidaTests
{
    private static Partida CriarPartidaAgendada() =>
        Partida.Create("partida-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(1), rodada: 1, temporada: 2026);

    [Fact]
    public void Create_should_raise_PartidaAgendadaDomainEvent_and_start_agendada()
    {
        Partida partida = CriarPartidaAgendada();

        partida.DomainEvents.Should().ContainSingle(e => e is PartidaAgendadaDomainEvent);
        partida.Situacao.Should().Be(SituacaoDaPartida.Agendada);
    }

    [Fact]
    public void RegistrarResultado_should_set_gols_and_situacao_encerrada_and_raise_event()
    {
        Partida partida = CriarPartidaAgendada();
        partida.ClearDomainEvents();

        Result resultado = partida.RegistrarResultado(2, 1);

        resultado.IsSuccess.Should().BeTrue();
        partida.GolsCasa.Should().Be(2);
        partida.GolsVisitante.Should().Be(1);
        partida.Situacao.Should().Be(SituacaoDaPartida.Encerrada);
        partida.DomainEvents.Should().ContainSingle(e => e is PartidaEncerradaDomainEvent);
    }

    [Fact]
    public void RegistrarResultado_should_fail_when_partida_already_encerrada()
    {
        Partida partida = CriarPartidaAgendada();
        partida.RegistrarResultado(2, 1);

        Result resultado = partida.RegistrarResultado(3, 0);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(PartidaErrors.JaEncerrada(partida.Id));
    }

    [Fact]
    public void Reagendar_should_update_data_and_raise_event_when_data_is_different()
    {
        Partida partida = CriarPartidaAgendada();
        partida.ClearDomainEvents();
        DateTime novaData = DateTime.UtcNow.AddDays(5);

        Result resultado = partida.Reagendar(novaData);

        resultado.IsSuccess.Should().BeTrue();
        partida.DataUtc.Should().Be(novaData);
        partida.DomainEvents.Should().ContainSingle(e => e is PartidaReagendadaDomainEvent);
    }

    [Fact]
    public void Reagendar_should_be_a_no_op_when_data_is_unchanged()
    {
        Partida partida = CriarPartidaAgendada();
        partida.ClearDomainEvents();

        Result resultado = partida.Reagendar(partida.DataUtc);

        resultado.IsSuccess.Should().BeTrue();
        partida.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Reagendar_should_fail_when_partida_already_encerrada()
    {
        Partida partida = CriarPartidaAgendada();
        partida.RegistrarResultado(2, 1);

        Result resultado = partida.Reagendar(DateTime.UtcNow.AddDays(10));

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(PartidaErrors.JaEncerrada(partida.Id));
    }

    [Fact]
    public void MarcarComoLiquidada_should_succeed_and_raise_event_when_encerrada()
    {
        Partida partida = CriarPartidaAgendada();
        partida.RegistrarResultado(2, 1);
        partida.ClearDomainEvents();

        Result resultado = partida.MarcarComoLiquidada();

        resultado.IsSuccess.Should().BeTrue();
        partida.Situacao.Should().Be(SituacaoDaPartida.Liquidada);
        partida.DomainEvents.Should().ContainSingle(e => e is PartidaLiquidadaDomainEvent);
    }

    [Fact]
    public void MarcarComoLiquidada_should_fail_when_partida_not_yet_encerrada()
    {
        Partida partida = CriarPartidaAgendada();

        Result resultado = partida.MarcarComoLiquidada();

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(PartidaErrors.AindaNaoEncerrada(partida.Id));
    }
}
