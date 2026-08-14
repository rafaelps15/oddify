using Oddify.Common.Application.Messaging;
using Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;

namespace Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorTime;

public sealed record GetDesempenhoPorTimeQuery(Guid BancaId) : IQuery<IReadOnlyCollection<DesempenhoResponse>>;
