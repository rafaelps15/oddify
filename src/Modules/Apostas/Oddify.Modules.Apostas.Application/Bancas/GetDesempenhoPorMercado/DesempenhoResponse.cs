namespace Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;

// Reaproveitado por GetDesempenhoPorCampeonato e GetDesempenhoPorTime - mesmo shape de
// desempenho agregado, só muda a chave de agrupamento (mercado / campeonato / time).
public sealed record DesempenhoResponse(
    string Chave,
    int QuantidadeDeApostas,
    int Ganhas,
    int Perdidas,
    decimal Lucro,
    decimal? Roi);
