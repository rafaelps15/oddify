using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.Bancas.GetSugestaoDeStake;

// SaldoDaBanca vem direto do caller, não de um BancaId real — esta query é a calculadora de
// alavancagem "e se" do front (o usuário pode digitar um saldo hipotético, diferente da banca
// real dele), uma conta pura sem necessidade de tocar o banco. Probabilidade em fração (0-1),
// igual a AnaliseDisponivelParaAposta.ProbabilidadeConfirmada e MontarMultiplaCommandHandler —
// não em percentual, pra não haver dois formatos de probabilidade coexistindo no módulo.
// FracaoDeKelly é opcional (comparar Kelly cheio/meio/um-quarto) — null usa a fração fixa real
// (KellyCalculator.FracaoDeKelly), a mesma que MontarMultiplaCommandHandler usa pra apostar de
// verdade.
public sealed record GetSugestaoDeStakeQuery(decimal SaldoDaBanca, decimal Odd, decimal Probabilidade, decimal? FracaoDeKelly = null)
    : IQuery<SugestaoDeStakeResponse>;
