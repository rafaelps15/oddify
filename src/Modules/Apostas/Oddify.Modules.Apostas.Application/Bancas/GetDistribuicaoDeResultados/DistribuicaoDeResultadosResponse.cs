namespace Oddify.Modules.Apostas.Application.Bancas.GetDistribuicaoDeResultados;

public sealed record DistribuicaoDeResultadosResponse(Guid BancaId, int Green, int Red, int Anuladas);
