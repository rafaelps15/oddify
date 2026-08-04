namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetAnalisesDisponiveisParaAposta;

public sealed record AnaliseDisponivelParaApostaResponse(
    Guid Id,
    Guid PartidaId,
    string Mercado,
    decimal OddDeMercado,
    decimal ProbabilidadeConfirmada,
    decimal Vantagem,
    bool Reduzida);
