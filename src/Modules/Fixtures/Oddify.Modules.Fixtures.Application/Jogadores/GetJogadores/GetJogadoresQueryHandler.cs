using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Fixtures.Application.Jogadores.GetJogador;

namespace Oddify.Modules.Fixtures.Application.Jogadores.GetJogadores;

internal sealed class GetJogadoresQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetJogadoresQuery, IReadOnlyCollection<JogadorResponse>>
{
    public async Task<Result<IReadOnlyCollection<JogadorResponse>>> Handle(GetJogadoresQuery request, CancellationToken cancellationToken)
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
             WHERE equipe_id = @EquipeId
             ORDER BY nome
             """;

        IReadOnlyCollection<JogadorResponse> result = (await connection.QueryAsync<JogadorResponse>(sql, request)).AsList();

        return Result.Success(result);
    }
}
