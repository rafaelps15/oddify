using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Domain.Jogadores;

namespace Oddify.Modules.Fixtures.Application.Jogadores.GetJogador;

internal sealed class GetJogadorQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetJogadorQuery, JogadorResponse>
{
    public async Task<Result<JogadorResponse>> Handle(GetJogadorQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(JogadorResponse.Id)},
                 id_externo AS {nameof(JogadorResponse.IdExterno)},
                 equipe_id AS {nameof(JogadorResponse.EquipeId)},
                 nome AS {nameof(JogadorResponse.Nome)},
                 posicao AS {nameof(JogadorResponse.Posicao)}
             FROM fixtures.jogadores
             WHERE id = @JogadorId
             """;

        JogadorResponse? result = await connection.QuerySingleOrDefaultAsync<JogadorResponse>(sql, request);

        if (result is null)
        {
            return Result.Failure<JogadorResponse>(JogadorErrors.NotFound(request.JogadorId));
        }

        return result;
    }
}
