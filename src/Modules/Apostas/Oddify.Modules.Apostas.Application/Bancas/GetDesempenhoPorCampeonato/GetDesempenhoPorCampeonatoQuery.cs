using Oddify.Common.Application.Messaging;
using Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;

namespace Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorCampeonato;

public sealed record GetDesempenhoPorCampeonatoQuery(Guid BancaId) : IQuery<IReadOnlyCollection<DesempenhoResponse>>;
