using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;

namespace Oddify.Modules.Apostas.Application.Bancas.GetResumoDaBanca;

internal sealed class GetResumoDaBancaQueryHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetResumoDaBancaQuery, ResumoDaBancaResponse>
{
    public async Task<Result<ResumoDaBancaResponse>> Handle(GetResumoDaBancaQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        var parametros = new ResumoParametros(request.BancaId, userContext.UserId);

        string sql =
            $"""
             SELECT
                 b.id AS {nameof(ResumoDaBancaResponse.BancaId)},
                 b.saldo_atual AS {nameof(ResumoDaBancaResponse.SaldoAtual)},
                 COALESCE(SUM(CASE WHEN m.tipo = {(int)TipoDeMovimentacao.Deposito} THEN m.valor ELSE 0 END), 0)
                     AS {nameof(ResumoDaBancaResponse.TotalDepositado)},
                 COALESCE(SUM(CASE WHEN m.tipo = {(int)TipoDeMovimentacao.Liquidacao} AND m.valor > 0 THEN m.valor ELSE 0 END), 0)
                     AS {nameof(ResumoDaBancaResponse.TotalGanho)},
                 COALESCE(SUM(CASE WHEN m.tipo = {(int)TipoDeMovimentacao.Liquidacao} AND m.valor < 0 THEN m.valor ELSE 0 END), 0)
                     AS {nameof(ResumoDaBancaResponse.TotalPerdido)},
                 COUNT(DISTINCT m.aposta_multipla_id) FILTER (WHERE m.tipo = {(int)TipoDeMovimentacao.Liquidacao})
                     AS {nameof(ResumoDaBancaResponse.QuantidadeDeApostas)}
             FROM apostas.bancas b
             LEFT JOIN apostas.movimentacoes_da_banca m ON m.banca_id = b.id
             WHERE b.id = @BancaId AND b.usuario_id = @UsuarioId
             GROUP BY b.id, b.saldo_atual
             """;

        ResumoDaBancaResponse? resultado = await connection.QuerySingleOrDefaultAsync<ResumoDaBancaResponse>(sql, parametros);

        if (resultado is null)
        {
            return Result.Failure<ResumoDaBancaResponse>(BancaErrors.NotFound(request.BancaId));
        }

        return resultado;
    }

    private sealed record ResumoParametros(Guid BancaId, Guid UsuarioId);
}
