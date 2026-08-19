using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Clock;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Apostas.Domain.Bancas;
using Oddify.Modules.Apostas.Domain.MovimentacoesDaBanca;

namespace Oddify.Modules.Apostas.Application.Bancas.GetResumoDaBanca;

internal sealed class GetResumoDaBancaQueryHandler(
    IDbConnectionFactory dbConnectionFactory,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetResumoDaBancaQuery, ResumoDaBancaResponse>
{
    public async Task<Result<ResumoDaBancaResponse>> Handle(GetResumoDaBancaQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        DateTime? desdeUtc = request.Dias is int dias ? dateTimeProvider.UtcNow.AddDays(-dias) : null;

        var parametros = new ResumoParametros(
            request.BancaId,
            userContext.UserId,
            desdeUtc,
            (int)TipoDeMovimentacao.Deposito,
            (int)ResultadoDaAposta.Pendente,
            (int)ResultadoDaAposta.Ganha,
            (int)ResultadoDaAposta.Anulada,
            (int)ResultadoDaAposta.MeioGanha);

        // Depósitos e apostas resolvidas são agregados em subconsultas independentes (nunca um
        // JOIN direto entre movimentacoes_da_banca e apostas_multiplas na mesma linha) — cada
        // banca tem N movimentações e M apostas de forma independente, então um join simples entre
        // as duas multiplicaria as linhas (fan-out) e inflaria todas as somas.
        const string sql =
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
                     AS {nameof(ResumoDaBancaResponse.Assertividade)},
                 CASE WHEN mov_inicio.saldo_inicio IS NOT NULL THEN b.saldo_atual - mov_inicio.saldo_inicio ELSE NULL END
                     AS {nameof(ResumoDaBancaResponse.VariacaoAbsoluta)},
                 CASE WHEN COALESCE(mov_inicio.saldo_inicio, 0) > 0
                     THEN (b.saldo_atual - mov_inicio.saldo_inicio) / mov_inicio.saldo_inicio * 100
                     ELSE NULL END
                     AS {nameof(ResumoDaBancaResponse.VariacaoPercentual)}
             FROM apostas.bancas b
             LEFT JOIN (
                 SELECT banca_id, SUM(valor) AS total
                 FROM apostas.movimentacoes_da_banca
                 WHERE tipo = @Deposito
                 GROUP BY banca_id
             ) dep ON dep.banca_id = b.id
             LEFT JOIN (
                 SELECT
                     banca_id,
                     SUM(CASE WHEN lucro_ou_perda > 0 THEN lucro_ou_perda ELSE 0 END) AS total_ganho,
                     SUM(CASE WHEN lucro_ou_perda < 0 THEN lucro_ou_perda ELSE 0 END) AS total_perdido,
                     SUM(COALESCE(lucro_ou_perda, 0)) AS lucro,
                     SUM(stake) FILTER (WHERE resultado != @Pendente) AS total_apostado,
                     COUNT(*) FILTER (WHERE resultado NOT IN (@Pendente, @Anulada)) AS decididas,
                     COUNT(*) FILTER (WHERE resultado IN (@Ganha, @MeioGanha)) AS ganhas,
                     COUNT(*) FILTER (WHERE resultado != @Pendente) AS quantidade
                 FROM apostas.apostas_multiplas
                 GROUP BY banca_id
             ) am ON am.banca_id = b.id
             LEFT JOIN LATERAL (
                 SELECT m.saldo_apos_movimentacao AS saldo_inicio
                 FROM apostas.movimentacoes_da_banca m
                 WHERE m.banca_id = b.id AND (@DesdeUtc IS NULL OR m.criada_em_utc >= @DesdeUtc)
                 ORDER BY m.criada_em_utc ASC
                 LIMIT 1
             ) mov_inicio ON TRUE
             WHERE b.id = @BancaId AND b.usuario_id = @UsuarioId
             """;

        ResumoDaBancaResponse? resultado = await connection.QuerySingleOrDefaultAsync<ResumoDaBancaResponse>(sql, parametros);

        if (resultado is null)
        {
            return Result.Failure<ResumoDaBancaResponse>(BancaErrors.NotFound(request.BancaId));
        }

        return resultado;
    }

    private sealed record ResumoParametros(
        Guid BancaId,
        Guid UsuarioId,
        DateTime? DesdeUtc,
        int Deposito,
        int Pendente,
        int Ganha,
        int Anulada,
        int MeioGanha);
}
