using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;

public sealed record GetDesempenhoPorMercadoQuery(Guid BancaId) : IQuery<IReadOnlyCollection<DesempenhoResponse>>;
