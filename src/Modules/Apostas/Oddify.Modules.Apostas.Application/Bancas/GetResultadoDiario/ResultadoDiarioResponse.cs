namespace Oddify.Modules.Apostas.Application.Bancas.GetResultadoDiario;

// Um dia sem nenhuma aposta resolvida simplesmente não aparece na coleção - cabe ao consumidor
// tratar os dias ausentes do mês como "sem apostas" (nem lucro, nem prejuízo).
public sealed record ResultadoDiarioResponse(DateTime Data, decimal Lucro, int QuantidadeDeApostas);
