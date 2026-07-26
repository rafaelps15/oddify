using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.Modules.Apostas.Application.Bancas.GetBanca;

internal sealed class GetBancaQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetBancaQuery, BancaResponse>
{
    public async Task<Result<BancaResponse>> Handle(GetBancaQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(BancaResponse.Id)},
                 saldo_atual AS {nameof(BancaResponse.SaldoAtual)},
                 modo_paper_trading AS {nameof(BancaResponse.ModoPaperTrading)}
             FROM apostas.bancas
             WHERE id = @BancaId
             """;

        BancaResponse? result = await connection.QuerySingleOrDefaultAsync<BancaResponse>(sql, request);

        if (result is null)
        {
            return Result.Failure<BancaResponse>(BancaErrors.NotFound(request.BancaId));
        }

        return result;
    }
}
