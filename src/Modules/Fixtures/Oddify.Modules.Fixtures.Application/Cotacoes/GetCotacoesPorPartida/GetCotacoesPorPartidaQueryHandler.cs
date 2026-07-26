using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Application.Cotacoes.GetCotacoesPorPartida;

internal sealed class GetCotacoesPorPartidaQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetCotacoesPorPartidaQuery, IReadOnlyCollection<CotacaoResponse>>
{
    public async Task<Result<IReadOnlyCollection<CotacaoResponse>>> Handle(
        GetCotacoesPorPartidaQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(CotacaoResponse.Id)},
                 partida_id AS {nameof(CotacaoResponse.PartidaId)},
                 mercado AS {nameof(CotacaoResponse.Mercado)},
                 odd AS {nameof(CotacaoResponse.Odd)},
                 casa AS {nameof(CotacaoResponse.Casa)},
                 coletada_em_utc AS {nameof(CotacaoResponse.ColetadaEmUtc)}
             FROM fixtures.cotacoes
             WHERE partida_id = @PartidaId
             ORDER BY coletada_em_utc DESC
             """;

        IReadOnlyCollection<CotacaoResponse> result = (await connection.QueryAsync<CotacaoResponse>(sql, request)).AsList();

        return Result.Success(result);
    }
}
