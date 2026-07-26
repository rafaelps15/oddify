using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetPartida;

public sealed record GetPartidaQuery(Guid PartidaId) : IQuery<PartidaResponse>;
