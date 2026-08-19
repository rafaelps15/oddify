namespace Oddify.Modules.Apostas.Application.Bancas.GetResumoDaBanca;

public sealed record ResumoDaBancaResponse(
    Guid BancaId,
    decimal SaldoAtual,
    decimal TotalDepositado,
    decimal TotalGanho,
    decimal TotalPerdido,
    int QuantidadeDeApostas,
    decimal Lucro,
    decimal? Roi,
    decimal? Assertividade,
    decimal PercentualPorEntrada,
    decimal ValorDaUnidade,
    // Null quando a banca não tem nenhuma movimentação dentro da janela pedida (GetResumoDaBancaQuery.Dias)
    // — "sem dado", nunca 0. VariacaoPercentual também null quando o saldo no início da janela é zero
    // (divisão por zero não faz sentido de domínio aqui).
    decimal? VariacaoAbsoluta,
    decimal? VariacaoPercentual);
