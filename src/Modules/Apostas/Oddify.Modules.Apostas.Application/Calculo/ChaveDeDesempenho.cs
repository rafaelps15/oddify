using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Apostas.Application.Calculo;

// Fonte única da regra "como agrupar uma aposta nos relatórios de desempenho": o lucro/perda de
// uma aposta com mais de uma perna não é atribuível a um mercado, campeonato ou time específico
// (ela combina vários), então essas apostas caem sempre na chave Multipla em vez de inflar a
// contagem de cada mercado/campeonato/time que ela tocou. Usado por GetDesempenhoPorMercado,
// GetDesempenhoPorCampeonato e GetDesempenhoPorTime — nenhum dos três decide isso por conta própria.
internal static class ChaveDeDesempenho
{
    public const string Multipla = "Múltipla";
    public const string Desconhecida = "Outros";

    // Perna única entra no campeonato real da partida; múltipla cai sempre em Multipla.
    public static string ResolverPorCampeonato(
        int qtdPernas,
        Guid partidaId,
        IReadOnlyDictionary<Guid, PartidaResumoResponse> partidasPorId)
    {
        if (qtdPernas > 1)
        {
            return Multipla;
        }

        return partidasPorId.TryGetValue(partidaId, out PartidaResumoResponse? partida) ? partida.LigaNome : Desconhecida;
    }

    // Perna única conta pro desempenho dos dois times da partida (o mercado nem sempre é
    // exclusivo de um dos lados, ex.: total de gols/escanteios); múltipla cai sempre em Multipla.
    public static IReadOnlyCollection<string> ResolverPorTime(
        int qtdPernas,
        Guid partidaId,
        IReadOnlyDictionary<Guid, PartidaResumoResponse> partidasPorId)
    {
        if (qtdPernas > 1)
        {
            return [Multipla];
        }

        if (!partidasPorId.TryGetValue(partidaId, out PartidaResumoResponse? partida))
        {
            return [Desconhecida];
        }

        return [partida.EquipeCasaNome, partida.EquipeVisitanteNome];
    }
}
