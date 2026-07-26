using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;

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
                 id AS {nameof(ApostaMultiplaResponse.Id)},
                 banca_id AS {nameof(ApostaMultiplaResponse.BancaId)},
                 odd_combinada AS {nameof(ApostaMultiplaResponse.OddCombinada)},
                 stake AS {nameof(ApostaMultiplaResponse.Stake)},
                 resultado AS {nameof(ApostaMultiplaResponse.Resultado)},
                 lucro_ou_perda AS {nameof(ApostaMultiplaResponse.LucroOuPerda)},
                 criada_em_utc AS {nameof(ApostaMultiplaResponse.CriadaEmUtc)}
             FROM apostas.apostas_multiplas
             WHERE @BancaId IS NULL OR banca_id = @BancaId
             ORDER BY criada_em_utc DESC
             """;

        IReadOnlyCollection<ApostaMultiplaResponse> result = (await connection.QueryAsync<ApostaMultiplaResponse>(sql, request)).AsList();

        return Result.Success(result);
    }
}
