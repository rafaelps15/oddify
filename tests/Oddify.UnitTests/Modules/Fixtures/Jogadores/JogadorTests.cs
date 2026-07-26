using FluentAssertions;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Domain.Jogadores;

namespace Oddify.UnitTests.Modules.Fixtures.Jogadores;

public sealed class JogadorTests
{
    [Fact]
    public void Create_should_raise_JogadorCriadoDomainEvent()
    {
        var jogador = Jogador.Create("jogador-1", Guid.NewGuid(), "Bukayo Saka", "Atacante");

        jogador.DomainEvents.Should().ContainSingle(e => e is JogadorCriadoDomainEvent);
    }

    [Fact]
    public void TransferirParaEquipe_should_update_equipe_and_raise_event_when_equipe_is_different()
    {
        var equipeOriginal = Guid.NewGuid();
        var novaEquipe = Guid.NewGuid();
        var jogador = Jogador.Create("jogador-1", equipeOriginal, "Bukayo Saka", "Atacante");
        jogador.ClearDomainEvents();

        Result resultado = jogador.TransferirParaEquipe(novaEquipe);

        resultado.IsSuccess.Should().BeTrue();
        jogador.EquipeId.Should().Be(novaEquipe);
        jogador.DomainEvents.Should().ContainSingle(e => e is JogadorTransferidoDomainEvent);
    }

    [Fact]
    public void TransferirParaEquipe_should_fail_when_equipe_is_the_same()
    {
        var equipeId = Guid.NewGuid();
        var jogador = Jogador.Create("jogador-1", equipeId, "Bukayo Saka", "Atacante");

        Result resultado = jogador.TransferirParaEquipe(equipeId);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be(JogadorErrors.JaNaEquipe);
    }
}
