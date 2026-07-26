using Oddify.Common.Application.Messaging;
using Oddify.Modules.Fixtures.Application.Cotacoes.GetCotacoesPorPartida;

namespace Oddify.Modules.Fixtures.Application.Cotacoes.GetCotacaoMaisRecente;

public sealed record GetCotacaoMaisRecenteQuery(Guid PartidaId, string Mercado) : IQuery<CotacaoResponse>;
