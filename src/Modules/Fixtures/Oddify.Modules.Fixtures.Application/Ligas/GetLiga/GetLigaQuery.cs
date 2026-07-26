using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Ligas.GetLiga;

public sealed record GetLigaQuery(Guid LigaId) : IQuery<LigaResponse>;
