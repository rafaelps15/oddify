using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Application.EstatisticasDeJogador.GetEstatisticasDeJogador;

internal sealed class GetEstatisticasDeJogadorQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetEstatisticasDeJogadorQuery, IReadOnlyCollection<EstatisticaJogadorResponse>>
{
    public async Task<Result<IReadOnlyCollection<EstatisticaJogadorResponse>>> Handle(
        GetEstatisticasDeJogadorQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(EstatisticaJogadorResponse.Id)},
                 partida_id AS {nameof(EstatisticaJogadorResponse.PartidaId)},
                 jogador_id AS {nameof(EstatisticaJogadorResponse.JogadorId)},
                 gols AS {nameof(EstatisticaJogadorResponse.Gols)},
                 assistencias AS {nameof(EstatisticaJogadorResponse.Assistencias)},
                 minutos AS {nameof(EstatisticaJogadorResponse.Minutos)},
                 titular AS {nameof(EstatisticaJogadorResponse.Titular)},
                 nota AS {nameof(EstatisticaJogadorResponse.Nota)}
             FROM fixtures.estatisticas_de_jogador
             WHERE partida_id = @PartidaId
             """;

        List<EstatisticaJogadorResponse> result = (await connection.QueryAsync<EstatisticaJogadorResponse>(sql, request)).AsList();

        return result;
    }
}
