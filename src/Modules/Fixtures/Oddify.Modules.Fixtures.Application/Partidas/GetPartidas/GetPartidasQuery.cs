using Oddify.Common.Application.Messaging;
using Oddify.Modules.Fixtures.Application.Partidas.GetPartida;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetPartidas;

public enum StatusFiltroDePartida
{
    Todas = 0,
    Agendadas = 1,

    // "Encerradas" agrupa Encerrada + Liquidada (jogo terminou, processado ou não) — a mesma
    // combinação que a tela de Partidas usa pro card "Encerradas" do resumo rápido.
    Encerradas = 2,

    // Situacao.EmAndamento (ver SincronizarAoVivo) — só o jogo rolando agora, nunca inclui
    // Agendada/Encerrada/Liquidada.
    AoVivo = 3
}

public sealed record GetPartidasQuery(
    Guid? LigaId,
    StatusFiltroDePartida Status = StatusFiltroDePartida.Todas,
    int? Rodada = null,
    int? Temporada = null,
    // Busca em lote por id — combina com os demais filtros (todos null-safe, "AND"), mas o uso
    // real (resolver as partidas referenciadas pelas pernas de uma lista de apostas múltiplas)
    // sempre manda só Ids, com o resto null.
    Guid[]? Ids = null) : IQuery<IReadOnlyCollection<PartidaResponse>>;
