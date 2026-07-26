using Oddify.Common.Domain;
using Oddify.Modules.Analise.Domain.Analises;

namespace Oddify.Modules.Analise.Application.Abstractions.Llm;

public sealed record AnaliseContexto(
    Guid AnaliseId,
    Guid PartidaId,
    string Mercado,
    decimal ProbPoissonPura,
    decimal ProbDixonColes,
    decimal ProbImplicitaDaOdd,
    decimal Vantagem,
    decimal OddDeMercado,
    string? ContextoAdicional);

public sealed record VeredictoClaude(DecisaoDoClaude Decisao, string Justificativa, string RespostaBruta);

public interface IClaudeAvaliadorCriticoService
{
    Task<Result<VeredictoClaude>> AvaliarAsync(AnaliseContexto contexto, CancellationToken cancellationToken = default);
}
