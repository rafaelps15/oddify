using Oddify.Common.Application.Messaging;
using Oddify.Modules.Fixtures.Application.Partidas.GetPartida;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetPartidas;

public enum StatusFiltroDePartida
{
    Todas = 0,
    Agendadas = 1,

    // "Encerradas" agrupa Encerrada + Liquidada (jogo terminou, processado ou não) — a mesma
    // combinação que a tela de Partidas usa pro card "Encerradas" do resumo rápido.
    Encerradas = 2
}

public sealed record GetPartidasQuery(
    Guid? LigaId,
    StatusFiltroDePartida Status = StatusFiltroDePartida.Todas,
    int? Rodada = null,
    int? Temporada = null) : IQuery<IReadOnlyCollection<PartidaResponse>>;
