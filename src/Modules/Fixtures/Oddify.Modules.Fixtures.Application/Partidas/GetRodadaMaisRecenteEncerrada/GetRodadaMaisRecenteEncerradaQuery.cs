using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetRodadaMaisRecenteEncerrada;

// Resultado null = nenhuma rodada com todos os jogos Encerrada/Liquidada ainda (ex.: temporada
// mal começou) — a tela de Partidas trata isso como "sem rodada padrão pra mostrar em Encerradas"
// (ver regra de negócio 1B do pedido original: "a rodada completa mais recente finalizada").
public sealed record GetRodadaMaisRecenteEncerradaQuery(Guid? LigaId, int Temporada) : IQuery<int?>;
