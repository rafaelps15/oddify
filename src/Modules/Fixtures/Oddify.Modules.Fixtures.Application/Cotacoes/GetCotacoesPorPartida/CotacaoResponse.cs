namespace Oddify.Modules.Fixtures.Application.Cotacoes.GetCotacoesPorPartida;

public sealed record CotacaoResponse(Guid Id, Guid PartidaId, string Mercado, decimal Odd, string Casa, DateTime ColetadaEmUtc);
