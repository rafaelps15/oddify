using Oddify.Common.Application.Messaging;
using Oddify.Modules.Fixtures.Application.Partidas.GetPartida;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetPartidas;

public sealed record GetPartidasQuery(Guid? LigaId) : IQuery<IReadOnlyCollection<PartidaResponse>>;
