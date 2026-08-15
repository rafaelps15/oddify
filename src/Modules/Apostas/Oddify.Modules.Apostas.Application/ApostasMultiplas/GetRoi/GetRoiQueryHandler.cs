using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetRoi;

internal sealed class GetRoiQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetRoiQuery, RoiResponse>
{
    public async Task<Result<RoiResponse>> Handle(GetRoiQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 COALESCE(SUM(COALESCE(lucro_ou_perda, 0)), 0) AS {nameof(RoiResponse.LucroTotal)},
                 COALESCE(SUM(stake), 0) AS {nameof(RoiResponse.TotalApostado)},
                 CASE WHEN SUM(stake) > 0 THEN SUM(COALESCE(lucro_ou_perda, 0)) / SUM(stake) ELSE NULL END
                     AS {nameof(RoiResponse.Roi)}
             FROM apostas.apostas_multiplas
             WHERE resultado != 0
               AND (@BancaId IS NULL OR banca_id = @BancaId)
             """;

        RoiResponse resultado = await connection.QuerySingleAsync<RoiResponse>(sql, request);

        return resultado;
    }
}
