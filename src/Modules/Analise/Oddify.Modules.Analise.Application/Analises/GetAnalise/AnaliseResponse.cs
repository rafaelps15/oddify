using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.Modules.Analise.Application.Analises.GetAnalise;

public sealed record AnaliseResponse(
    Guid Id,
    Guid PartidaId,
    string Mercado,
    decimal ProbPoissonPura,
    decimal ProbDixonColes,
    decimal ProbImplicitaDaOdd,
    decimal Vantagem,
    decimal OddDeMercado,
    bool AprovadaNoFiltro,
    string? MotivoDoDescarte,
    DecisaoDoClaude DecisaoDoClaude,
    string? JustificativaDoClaude,
    string? VersaoDoPrompt,
    DateTime CriadaEmUtc);
