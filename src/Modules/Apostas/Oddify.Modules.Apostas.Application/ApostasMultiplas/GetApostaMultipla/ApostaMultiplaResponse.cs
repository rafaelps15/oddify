using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;

public sealed record ApostaMultiplaResponse(
    Guid Id,
    Guid BancaId,
    decimal OddCombinada,
    decimal Stake,
    ResultadoDaAposta Resultado,
    decimal? LucroOuPerda,
    DateTime CriadaEmUtc,
    IReadOnlyCollection<PernaResponse> Pernas);

// Shape só das colunas de apostas.apostas_multiplas — GetApostaMultiplaQueryHandler e
// GetApostasMultiplasQueryHandler nunca selecionam "Pernas" nessa consulta (buscam as pernas à
// parte, numa segunda consulta) e Dapper exige que os parâmetros do construtor batam exatamente
// com as colunas lidas pra materializar por construtor — um ApostaMultiplaResponse com 8
// parâmetros não bate com um SELECT de 7 colunas mesmo com valor padrão. Esse record intermediário
// é só o passo de leitura; os handlers montam o ApostaMultiplaResponse de verdade depois de buscar
// as pernas.
internal sealed record ApostaMultiplaRow(
    Guid Id,
    Guid BancaId,
    decimal OddCombinada,
    decimal Stake,
    ResultadoDaAposta Resultado,
    decimal? LucroOuPerda,
    DateTime CriadaEmUtc)
{
    public ApostaMultiplaResponse ToResponse(IReadOnlyCollection<PernaResponse> pernas) =>
        new(Id, BancaId, OddCombinada, Stake, Resultado, LucroOuPerda, CriadaEmUtc, pernas);
}
