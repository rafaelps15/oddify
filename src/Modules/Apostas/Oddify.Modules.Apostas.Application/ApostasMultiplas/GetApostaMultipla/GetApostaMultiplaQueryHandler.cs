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
                 id AS {nameof(ApostaMultiplaRow.Id)},
                 banca_id AS {nameof(ApostaMultiplaRow.BancaId)},
                 odd_combinada AS {nameof(ApostaMultiplaRow.OddCombinada)},
                 stake AS {nameof(ApostaMultiplaRow.Stake)},
                 resultado AS {nameof(ApostaMultiplaRow.Resultado)},
                 lucro_ou_perda AS {nameof(ApostaMultiplaRow.LucroOuPerda)},
                 criada_em_utc AS {nameof(ApostaMultiplaRow.CriadaEmUtc)}
             FROM apostas.apostas_multiplas
             WHERE id = @ApostaMultiplaId
             """;

        ApostaMultiplaRow? row = await connection.QuerySingleOrDefaultAsync<ApostaMultiplaRow>(sql, request);

        if (row is null)
        {
            return Result.Failure<ApostaMultiplaResponse>(ApostaMultiplaErrors.NotFound(request.ApostaMultiplaId));
        }

        IReadOnlyCollection<PernaResponse> pernas = await GetPernasAsync(connection, row.Id);

        return row.ToResponse(pernas);
    }

    private static async Task<IReadOnlyCollection<PernaResponse>> GetPernasAsync(DbConnection connection, Guid apostaMultiplaId)
    {
        const string sql =
            $"""
             SELECT
                 id AS {nameof(PernaResponse.Id)},
                 mercado AS {nameof(PernaResponse.Mercado)},
                 odd AS {nameof(PernaResponse.Odd)},
                 partida_id AS {nameof(PernaResponse.PartidaId)},
                 resultado AS {nameof(PernaResponse.Resultado)}
             FROM apostas.pernas_de_aposta
             WHERE aposta_multipla_id = @ApostaMultiplaId
             """;

        List<PernaResponse> pernas = (await connection.QueryAsync<PernaResponse>(sql, new { ApostaMultiplaId = apostaMultiplaId })).AsList();
        return pernas;
    }
}
