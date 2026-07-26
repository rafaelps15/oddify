using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;

internal sealed class GetApostaMultiplaQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetApostaMultiplaQuery, ApostaMultiplaResponse>
{
    public async Task<Result<ApostaMultiplaResponse>> Handle(GetApostaMultiplaQuery request, CancellationToken cancellationToken)
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
             WHERE id = @ApostaMultiplaId
             """;

        ApostaMultiplaResponse? result = await connection.QuerySingleOrDefaultAsync<ApostaMultiplaResponse>(sql, request);

        if (result is null)
        {
            return Result.Failure<ApostaMultiplaResponse>(ApostaMultiplaErrors.NotFound(request.ApostaMultiplaId));
        }

        return result;
    }
}
