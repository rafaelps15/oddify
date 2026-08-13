using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostasMultiplas;

internal sealed class GetApostasMultiplasQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetApostasMultiplasQuery, IReadOnlyCollection<ApostaMultiplaResponse>>
{
    public async Task<Result<IReadOnlyCollection<ApostaMultiplaResponse>>> Handle(
        GetApostasMultiplasQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(ApostaMultiplaRow.Id)},
                 banca_id AS {nameof(ApostaMultiplaRow.BancaId)},
                 odd_combinada AS {nameof(ApostaMultiplaRow.OddCombinada)},
                 stake AS {nameof(ApostaMultiplaRow.Stake)},
                 resultado AS {nameof(ApostaMultiplaRow.Resultado)},
                 lucro_ou_perda AS {nameof(ApostaMultiplaRow.LucroOuPerda)},
                 criada_em_utc AS {nameof(ApostaMultiplaRow.CriadaEmUtc)}
             FROM apostas.apostas_multiplas
             WHERE @BancaId IS NULL OR banca_id = @BancaId
             ORDER BY criada_em_utc DESC
             """;

        List<ApostaMultiplaRow> apostas = (await connection.QueryAsync<ApostaMultiplaRow>(sql, request)).AsList();

        if (apostas.Count == 0)
        {
            return Result.Success<IReadOnlyCollection<ApostaMultiplaResponse>>([]);
        }

        ILookup<Guid, PernaResponse> pernasPorAposta = await GetPernasAsync(connection, apostas.Select(a => a.Id));

        IReadOnlyCollection<ApostaMultiplaResponse> result = apostas
            .Select(aposta => aposta.ToResponse(pernasPorAposta[aposta.Id].ToList()))
            .ToList();

        return Result.Success(result);
    }

    // Uma única consulta pra todas as apostas da página em vez de uma por aposta (evita N+1) —
    // agrupada em memória depois, já que Dapper não faz o join direto pro shape aninhado de
    // ApostaMultiplaResponse.
    private static async Task<ILookup<Guid, PernaResponse>> GetPernasAsync(DbConnection connection, IEnumerable<Guid> apostaMultiplaIds)
    {
        const string sql =
            $"""
             SELECT
                 aposta_multipla_id AS {nameof(PernaComAposta.ApostaMultiplaId)},
                 id AS {nameof(PernaComAposta.Id)},
                 mercado AS {nameof(PernaComAposta.Mercado)},
                 odd AS {nameof(PernaComAposta.Odd)},
                 partida_id AS {nameof(PernaComAposta.PartidaId)},
                 resultado AS {nameof(PernaComAposta.Resultado)}
             FROM apostas.pernas_de_aposta
             WHERE aposta_multipla_id IN @ApostaMultiplaIds
             """;

        IEnumerable<PernaComAposta> pernas =
            await connection.QueryAsync<PernaComAposta>(sql, new { ApostaMultiplaIds = apostaMultiplaIds });

        return pernas.ToLookup(
            p => p.ApostaMultiplaId,
            p => new PernaResponse(p.Id, p.Mercado, p.Odd, p.PartidaId, p.Resultado));
    }

    private sealed record PernaComAposta(Guid ApostaMultiplaId, Guid Id, string Mercado, decimal Odd, Guid PartidaId, ResultadoDaAposta Resultado);
}
