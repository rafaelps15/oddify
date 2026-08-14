using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.Modules.Apostas.Application.Bancas.GetDistribuicaoDeResultados;

internal sealed class GetDistribuicaoDeResultadosQueryHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetDistribuicaoDeResultadosQuery, DistribuicaoDeResultadosResponse>
{
    public async Task<Result<DistribuicaoDeResultadosResponse>> Handle(
        GetDistribuicaoDeResultadosQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        var parametros = new DistribuicaoParametros(request.BancaId, userContext.UserId);

        string sql =
            $"""
             SELECT
                 b.id AS {nameof(DistribuicaoDeResultadosResponse.BancaId)},
                 COALESCE(COUNT(am.id) FILTER (WHERE am.resultado = {(int)ResultadoDaAposta.Ganha}), 0)::int
                     AS {nameof(DistribuicaoDeResultadosResponse.Green)},
                 COALESCE(COUNT(am.id) FILTER (WHERE am.resultado = {(int)ResultadoDaAposta.Perdida}), 0)::int
                     AS {nameof(DistribuicaoDeResultadosResponse.Red)},
                 COALESCE(COUNT(am.id) FILTER (WHERE am.resultado = {(int)ResultadoDaAposta.Anulada}), 0)::int
                     AS {nameof(DistribuicaoDeResultadosResponse.Anuladas)}
             FROM apostas.bancas b
             LEFT JOIN apostas.apostas_multiplas am ON am.banca_id = b.id
             WHERE b.id = @BancaId AND b.usuario_id = @UsuarioId
             GROUP BY b.id
             """;

        DistribuicaoDeResultadosResponse? resultado =
            await connection.QuerySingleOrDefaultAsync<DistribuicaoDeResultadosResponse>(sql, parametros);

        if (resultado is null)
        {
            return Result.Failure<DistribuicaoDeResultadosResponse>(BancaErrors.NotFound(request.BancaId));
        }

        return resultado;
    }

    private sealed record DistribuicaoParametros(Guid BancaId, Guid UsuarioId);
}
