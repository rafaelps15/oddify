using FluentAssertions;
using Oddify.Modules.Analise.Application.Calculo;
using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.UnitTests.Modules.Analise.Calculo;

public sealed class HistoricoDeEquipeCalculatorTests
{
    [Fact]
    public void Calcular_should_return_zeroed_historico_when_there_are_no_recent_games()
    {
        HistoricoDeEquipe resultado = HistoricoDeEquipeCalculator.Calcular([], Guid.NewGuid());

        resultado.AmostraDeJogos.Should().Be(0);
        resultado.MediaGolsFeitos.Should().Be(0m);
        resultado.MediaGolsSofridos.Should().Be(0m);
    }

    [Fact]
    public void Calcular_should_count_goals_scored_when_team_played_at_home()
    {
        var equipeId = Guid.NewGuid();
        var jogo = Partida.Create(Guid.NewGuid(), Guid.NewGuid(), equipeId, Guid.NewGuid(), DateTime.UtcNow);
        jogo.RegistrarResultado(golsCasa: 3, golsVisitante: 1);

        HistoricoDeEquipe resultado = HistoricoDeEquipeCalculator.Calcular([jogo], equipeId);

        resultado.AmostraDeJogos.Should().Be(1);
        resultado.MediaGolsFeitos.Should().Be(3m);
        resultado.MediaGolsSofridos.Should().Be(1m);
    }

    [Fact]
    public void Calcular_should_count_goals_scored_when_team_played_away()
    {
        var equipeId = Guid.NewGuid();
        var jogo = Partida.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), equipeId, DateTime.UtcNow);
        jogo.RegistrarResultado(golsCasa: 3, golsVisitante: 1);

        HistoricoDeEquipe resultado = HistoricoDeEquipeCalculator.Calcular([jogo], equipeId);

        resultado.AmostraDeJogos.Should().Be(1);
        resultado.MediaGolsFeitos.Should().Be(1m);
        resultado.MediaGolsSofridos.Should().Be(3m);
    }

    [Fact]
    public void Calcular_should_average_across_multiple_games_home_and_away()
    {
        var equipeId = Guid.NewGuid();

        var jogoEmCasa = Partida.Create(Guid.NewGuid(), Guid.NewGuid(), equipeId, Guid.NewGuid(), DateTime.UtcNow);
        jogoEmCasa.RegistrarResultado(golsCasa: 4, golsVisitante: 0);

        var jogoFora = Partida.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), equipeId, DateTime.UtcNow);
        jogoFora.RegistrarResultado(golsCasa: 2, golsVisitante: 2);

        HistoricoDeEquipe resultado = HistoricoDeEquipeCalculator.Calcular([jogoEmCasa, jogoFora], equipeId);

        resultado.AmostraDeJogos.Should().Be(2);
        resultado.MediaGolsFeitos.Should().Be(3m);
        resultado.MediaGolsSofridos.Should().Be(1m);
    }
}
