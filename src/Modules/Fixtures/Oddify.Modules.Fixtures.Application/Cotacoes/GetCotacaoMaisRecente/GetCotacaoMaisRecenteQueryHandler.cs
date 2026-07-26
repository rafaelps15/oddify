using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Cotacoes.GetCotacoesPorPartida;
using Oddify.Modules.Fixtures.Domain.Cotacoes;

namespace Oddify.Modules.Fixtures.Application.Cotacoes.GetCotacaoMaisRecente;

internal sealed class GetCotacaoMaisRecenteQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetCotacaoMaisRecenteQuery, CotacaoResponse>
{
    public async Task<Result<CotacaoResponse>> Handle(GetCotacaoMaisRecenteQuery request, CancellationToken cancellationToken)
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
             WHERE partida_id = @PartidaId AND mercado = @Mercado
             ORDER BY coletada_em_utc DESC
             LIMIT 1
             """;

        CotacaoResponse? result = await connection.QuerySingleOrDefaultAsync<CotacaoResponse>(sql, request);

        if (result is null)
        {
            return Result.Failure<CotacaoResponse>(CotacaoErrors.NenhumaCotacaoEncontrada(request.PartidaId, request.Mercado));
        }

        return result;
    }
}
