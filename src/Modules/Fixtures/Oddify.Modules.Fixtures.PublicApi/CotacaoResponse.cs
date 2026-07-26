namespace Oddify.Modules.Fixtures.PublicApi;

public sealed record CotacaoResponse(Guid Id, Guid PartidaId, string Mercado, decimal Odd, string Casa, DateTime ColetadaEmUtc);
