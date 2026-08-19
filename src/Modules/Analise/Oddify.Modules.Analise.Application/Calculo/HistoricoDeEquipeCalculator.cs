using Oddify.Modules.Analise.Domain.Fixtures;

namespace Oddify.Modules.Analise.Application.Calculo;

// Antes vinha pronto do Fixtures via IFixturesApi; agora Analise espelha os placares localmente
// (PartidaAgendadaIntegrationEvent/PartidaEncerradaIntegrationEvent) e calcula a média por conta
// própria — ver CLAUDE.md §17 (nada de Service, cálculo puro fica num Calculator estático).
public static class HistoricoDeEquipeCalculator
{
    public static HistoricoDeEquipe Calcular(IReadOnlyCollection<Partida> jogosRecentes, Guid equipeId)
    {
        if (jogosRecentes.Count == 0)
        {
            return new HistoricoDeEquipe(0, 0m, 0m);
        }

        decimal totalGolsFeitos = 0m;
        decimal totalGolsSofridos = 0m;

        foreach (Partida jogo in jogosRecentes)
        {
            bool jogouEmCasa = jogo.EquipeCasaId == equipeId;

            totalGolsFeitos += jogouEmCasa ? jogo.GolsCasa!.Value : jogo.GolsVisitante!.Value;
            totalGolsSofridos += jogouEmCasa ? jogo.GolsVisitante!.Value : jogo.GolsCasa!.Value;
        }

        return new HistoricoDeEquipe(
            jogosRecentes.Count,
            totalGolsFeitos / jogosRecentes.Count,
            totalGolsSofridos / jogosRecentes.Count);
    }
}

public sealed record HistoricoDeEquipe(int AmostraDeJogos, decimal MediaGolsFeitos, decimal MediaGolsSofridos);
