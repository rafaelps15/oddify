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
                 COALESCE(SUM(COALESCE(lucro_ou_perda, 0)), 0) AS {nameof(TotaisRow.LucroTotal)},
                 COALESCE(SUM(stake), 0) AS {nameof(TotaisRow.TotalApostado)}
             FROM apostas.apostas_multiplas
             WHERE resultado != 0
               AND (@BancaId IS NULL OR banca_id = @BancaId)
             """;

        TotaisRow totais = await connection.QuerySingleAsync<TotaisRow>(sql, request);

        decimal? roi = totais.TotalApostado > 0 ? totais.LucroTotal / totais.TotalApostado : null;

        return new RoiResponse(totais.LucroTotal, totais.TotalApostado, roi);
    }

    private sealed record TotaisRow(decimal LucroTotal, decimal TotalApostado);
}
