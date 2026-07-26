using Oddify.Common.Application.Messaging;
using Oddify.Modules.Fixtures.Application.Ligas.GetLiga;

namespace Oddify.Modules.Fixtures.Application.Ligas.GetLigas;

public sealed record GetLigasQuery : IQuery<IReadOnlyCollection<LigaResponse>>;
