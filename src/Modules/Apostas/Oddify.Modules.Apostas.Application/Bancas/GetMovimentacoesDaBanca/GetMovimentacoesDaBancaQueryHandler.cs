using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;

namespace Oddify.Modules.Apostas.Application.Bancas.GetMovimentacoesDaBanca;

internal sealed class GetMovimentacoesDaBancaQueryHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetMovimentacoesDaBancaQuery, GetMovimentacoesDaBancaResponse>
{
    public async Task<Result<GetMovimentacoesDaBancaResponse>> Handle(
        GetMovimentacoesDaBancaQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        int page = Math.Max(request.Page, 1);
        int pageSize = Math.Clamp(request.PageSize, 1, 100);

        var parametros = new MovimentacaoParametros(request.BancaId, userContext.UserId, (page - 1) * pageSize, pageSize);

        const string sqlItens =
            $"""
             SELECT
                 m.id AS {nameof(MovimentacaoDaBancaResponse.Id)},
                 m.tipo AS {nameof(MovimentacaoDaBancaResponse.Tipo)},
                 m.valor AS {nameof(MovimentacaoDaBancaResponse.Valor)},
                 m.saldo_apos_movimentacao AS {nameof(MovimentacaoDaBancaResponse.SaldoAposMovimentacao)},
                 m.aposta_multipla_id AS {nameof(MovimentacaoDaBancaResponse.ApostaMultiplaId)},
                 m.criada_em_utc AS {nameof(MovimentacaoDaBancaResponse.CriadaEmUtc)}
             FROM apostas.movimentacoes_da_banca m
             INNER JOIN apostas.bancas b ON b.id = m.banca_id
             WHERE m.banca_id = @BancaId AND b.usuario_id = @UsuarioId
             ORDER BY m.criada_em_utc DESC
             OFFSET @Skip LIMIT @PageSize
             """;

        const string sqlTotalCount =
            """
            SELECT COUNT(*)
            FROM apostas.movimentacoes_da_banca m
            INNER JOIN apostas.bancas b ON b.id = m.banca_id
            WHERE m.banca_id = @BancaId AND b.usuario_id = @UsuarioId
            """;

        IReadOnlyCollection<MovimentacaoDaBancaResponse> itens =
            (await connection.QueryAsync<MovimentacaoDaBancaResponse>(sqlItens, parametros)).AsList();
        int totalCount = await connection.QuerySingleAsync<int>(sqlTotalCount, parametros);

        return new GetMovimentacoesDaBancaResponse(page, pageSize, totalCount, itens);
    }

    private sealed record MovimentacaoParametros(Guid BancaId, Guid UsuarioId, int Skip, int PageSize);
}
