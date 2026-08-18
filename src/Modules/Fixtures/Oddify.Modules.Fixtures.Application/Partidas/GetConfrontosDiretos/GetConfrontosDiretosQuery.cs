using Oddify.Common.Application.Messaging;
using Oddify.Modules.Fixtures.Application.Partidas.GetPartida;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetConfrontosDiretos;

// Reaproveita PartidaResponse (já usado por GetPartida/GetPartidas) — o front deriva V/E/D e saldo
// de gols a partir de golsCasa/golsVisitante/equipeCasaId/equipeVisitanteId, então não há nenhuma
// projeção nova a definir aqui (query-slice.md §B1: "outras features de leitura que retornam a
// mesma forma referenciam a partir de lá, em vez de redefinir").
public sealed record GetConfrontosDiretosQuery(Guid EquipeAId, Guid EquipeBId, int Quantidade = 5)
    : IQuery<IReadOnlyCollection<PartidaResponse>>;
