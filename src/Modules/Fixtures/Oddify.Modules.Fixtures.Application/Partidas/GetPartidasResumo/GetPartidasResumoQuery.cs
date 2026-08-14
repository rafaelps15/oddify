using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetPartidasResumo;

// Busca em lote (sempre por Ids) - usada por outros modulos via IFixturesApi.ObterPartidasResumoAsync
// para resolver nome/escudo dos times de varias partidas de uma vez (evita N+1), nunca chamada
// diretamente por um endpoint proprio deste modulo.
public sealed record GetPartidasResumoQuery(Guid[] Ids) : IQuery<IReadOnlyCollection<PartidaResumoResponse>>;
