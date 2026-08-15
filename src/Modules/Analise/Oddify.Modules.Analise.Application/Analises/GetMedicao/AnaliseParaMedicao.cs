using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.Modules.Analise.Application.Analises.GetMedicao;

internal sealed record AnaliseParaMedicao(
    Guid PartidaId,
    string Mercado,
    decimal ProbPoissonPura,
    decimal ProbDixonColes,
    DecisaoDoClaude DecisaoDoClaude);
