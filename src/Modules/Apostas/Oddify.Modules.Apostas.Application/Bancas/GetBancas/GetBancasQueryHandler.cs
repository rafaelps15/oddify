using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Bancas.GetBanca;

namespace Oddify.Modules.Apostas.Application.Bancas.GetBancas;

internal sealed class GetBancasQueryHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetBancasQuery, IReadOnlyCollection<BancaResponse>>
{
    public async Task<Result<IReadOnlyCollection<BancaResponse>>> Handle(GetBancasQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(BancaResponse.Id)},
                 nome AS {nameof(BancaResponse.Nome)},
                 saldo_atual AS {nameof(BancaResponse.SaldoAtual)},
                 percentual_por_entrada AS {nameof(BancaResponse.PercentualPorEntrada)},
                 perfil_de_risco AS {nameof(BancaResponse.PerfilDeRisco)},
                 modo_paper_trading AS {nameof(BancaResponse.ModoPaperTrading)}
             FROM apostas.bancas
             WHERE usuario_id = @UserId AND ativa
             ORDER BY criado_em_utc
             """;

        List<BancaResponse> bancas = (await connection.QueryAsync<BancaResponse>(sql, new { userContext.UserId })).AsList();

        return bancas;
    }
}
