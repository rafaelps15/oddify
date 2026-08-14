using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.Modules.Apostas.Application.Bancas.GetResultadoDiario;

internal sealed class GetResultadoDiarioQueryHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetResultadoDiarioQuery, IReadOnlyCollection<ResultadoDiarioResponse>>
{
    public async Task<Result<IReadOnlyCollection<ResultadoDiarioResponse>>> Handle(
        GetResultadoDiarioQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        var parametros = new ResultadoDiarioParametros(request.BancaId, userContext.UserId, request.Ano, request.Mes);

        bool bancaExiste = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM apostas.bancas WHERE id = @BancaId AND usuario_id = @UsuarioId)", parametros);

        if (!bancaExiste)
        {
            return Result.Failure<IReadOnlyCollection<ResultadoDiarioResponse>>(BancaErrors.NotFound(request.BancaId));
        }

        string sql =
            $"""
             SELECT
                 DATE(atualizado_em_utc) AS {nameof(ResultadoDiarioResponse.Data)},
                 COALESCE(SUM(lucro_ou_perda), 0) AS {nameof(ResultadoDiarioResponse.Lucro)},
                 COUNT(*)::int AS {nameof(ResultadoDiarioResponse.QuantidadeDeApostas)}
             FROM apostas.apostas_multiplas
             WHERE banca_id = @BancaId
               AND usuario_id = @UsuarioId
               AND resultado IN (1, 2)
               AND EXTRACT(YEAR FROM atualizado_em_utc) = @Ano
               AND EXTRACT(MONTH FROM atualizado_em_utc) = @Mes
             GROUP BY DATE(atualizado_em_utc)
             ORDER BY DATE(atualizado_em_utc)
             """;

        IReadOnlyCollection<ResultadoDiarioResponse> resultado =
            (await connection.QueryAsync<ResultadoDiarioResponse>(sql, parametros)).AsList();

        return Result.Success(resultado);
    }

    private sealed record ResultadoDiarioParametros(Guid BancaId, Guid UsuarioId, int Ano, int Mes);
}
