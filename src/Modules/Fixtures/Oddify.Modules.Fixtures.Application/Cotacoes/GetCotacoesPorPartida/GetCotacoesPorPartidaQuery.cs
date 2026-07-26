using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Cotacoes.GetCotacoesPorPartida;

public sealed record GetCotacoesPorPartidaQuery(Guid PartidaId) : IQuery<IReadOnlyCollection<CotacaoResponse>>;
