using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
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

        // Depósitos e apostas resolvidas são agregados em subconsultas independentes (nunca um
        // JOIN direto entre movimentacoes_da_banca e apostas_multiplas na mesma linha) — cada
        // banca tem N movimentações e M apostas de forma independente, então um join simples entre
        // as duas multiplicaria as linhas (fan-out) e inflaria todas as somas.
        string sql =
            $"""
             SELECT
                 b.id AS {nameof(ResumoDaBancaResponse.BancaId)},
                 b.saldo_atual AS {nameof(ResumoDaBancaResponse.SaldoAtual)},
                 b.percentual_por_entrada AS {nameof(ResumoDaBancaResponse.PercentualPorEntrada)},
                 b.saldo_atual * b.percentual_por_entrada AS {nameof(ResumoDaBancaResponse.ValorDaUnidade)},
                 COALESCE(dep.total, 0) AS {nameof(ResumoDaBancaResponse.TotalDepositado)},
                 COALESCE(am.total_ganho, 0) AS {nameof(ResumoDaBancaResponse.TotalGanho)},
                 COALESCE(am.total_perdido, 0) AS {nameof(ResumoDaBancaResponse.TotalPerdido)},
                 COALESCE(am.quantidade, 0)::int AS {nameof(ResumoDaBancaResponse.QuantidadeDeApostas)},
                 COALESCE(am.lucro, 0) AS {nameof(ResumoDaBancaResponse.Lucro)},
                 CASE WHEN COALESCE(am.total_apostado, 0) > 0 THEN am.lucro / am.total_apostado ELSE NULL END
                     AS {nameof(ResumoDaBancaResponse.Roi)},
                 CASE WHEN COALESCE(am.decididas, 0) > 0 THEN am.ganhas::decimal / am.decididas ELSE NULL END
                     AS {nameof(ResumoDaBancaResponse.Assertividade)}
             FROM apostas.bancas b
             LEFT JOIN (
                 SELECT banca_id, SUM(valor) AS total
                 FROM apostas.movimentacoes_da_banca
                 WHERE tipo = {(int)TipoDeMovimentacao.Deposito}
                 GROUP BY banca_id
             ) dep ON dep.banca_id = b.id
             LEFT JOIN (
                 SELECT
                     banca_id,
                     SUM(CASE WHEN lucro_ou_perda > 0 THEN lucro_ou_perda ELSE 0 END) AS total_ganho,
                     SUM(CASE WHEN lucro_ou_perda < 0 THEN lucro_ou_perda ELSE 0 END) AS total_perdido,
                     SUM(COALESCE(lucro_ou_perda, 0)) AS lucro,
                     SUM(stake) FILTER (WHERE resultado != {(int)ResultadoDaAposta.Pendente}) AS total_apostado,
                     COUNT(*) FILTER (WHERE resultado != {(int)ResultadoDaAposta.Pendente}) AS decididas,
                     COUNT(*) FILTER (WHERE resultado = {(int)ResultadoDaAposta.Ganha}) AS ganhas,
                     COUNT(*) FILTER (WHERE resultado != {(int)ResultadoDaAposta.Pendente}) AS quantidade
                 FROM apostas.apostas_multiplas
                 GROUP BY banca_id
             ) am ON am.banca_id = b.id
             WHERE b.id = @BancaId AND b.usuario_id = @UsuarioId
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
