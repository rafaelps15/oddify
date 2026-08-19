using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.Bancas.GetResumoDaBanca;

// Dias é a janela pra VariacaoAbsoluta/VariacaoPercentual (7/30/null="tudo", mesmo seletor de
// PeriodoEvolucao no front) — null não filtra, olha o histórico inteiro de movimentações da banca.
public sealed record GetResumoDaBancaQuery(Guid BancaId, int? Dias = null) : IQuery<ResumoDaBancaResponse>;
