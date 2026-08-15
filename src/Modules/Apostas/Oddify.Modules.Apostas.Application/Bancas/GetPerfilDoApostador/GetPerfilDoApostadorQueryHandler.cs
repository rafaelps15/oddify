using System.Data.Common;
using Dapper;
using MediatR;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Bancas.GetDesempenhoPorMercado;
using Oddify.Modules.Apostas.Application.Calculo;
using Oddify.Modules.Apostas.Domain.Bancas;

namespace Oddify.Modules.Apostas.Application.Bancas.GetPerfilDoApostador;

internal sealed class GetPerfilDoApostadorQueryHandler(IDbConnectionFactory dbConnectionFactory, ISender sender, IUserContext userContext)
    : IQueryHandler<GetPerfilDoApostadorQuery, PerfilDoApostadorResponse>
{
    public async Task<Result<PerfilDoApostadorResponse>> Handle(GetPerfilDoApostadorQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        var parametros = new PerfilParametros(request.BancaId, userContext.UserId);

        decimal? unidadeSugeridaOuNula = await connection.QuerySingleOrDefaultAsync<decimal?>(
            """
            SELECT saldo_atual * percentual_por_entrada
            FROM apostas.bancas
            WHERE id = @BancaId AND usuario_id = @UsuarioId
            """,
            parametros);

        if (unidadeSugeridaOuNula is not { } unidadeSugerida)
        {
            return Result.Failure<PerfilDoApostadorResponse>(BancaErrors.NotFound(request.BancaId));
        }

        // Mais recente primeiro - PerfilDoApostadorCalculator lê a sequência atual a partir do
        // início desta lista.
        List<ApostaRow> apostas = (await connection.QueryAsync<ApostaRow>(
            $"""
             SELECT
                 resultado AS {nameof(ApostaRow.Resultado)},
                 stake AS {nameof(ApostaRow.Stake)}
             FROM apostas.apostas_multiplas
             WHERE banca_id = @BancaId AND usuario_id = @UsuarioId AND resultado IN (1, 2)
             ORDER BY atualizado_em_utc DESC
             """,
            parametros)).AsList();

        Result<IReadOnlyCollection<DesempenhoResponse>> desempenhoPorMercadoResult =
            await sender.Send(new GetDesempenhoPorMercadoQuery(request.BancaId), cancellationToken);

        IReadOnlyCollection<DesempenhoResponse> desempenhoPorMercado =
            desempenhoPorMercadoResult.IsSuccess ? desempenhoPorMercadoResult.Value : [];

        PerfilCalculado perfil = PerfilDoApostadorCalculator.Calcular(apostas, unidadeSugerida, desempenhoPorMercado);

        return new PerfilDoApostadorResponse(
            request.BancaId,
            perfil.EntradaMedia,
            unidadeSugerida,
            perfil.DisciplinaDeStake,
            perfil.SequenciaAtualTipo,
            perfil.SequenciaAtualQuantidade,
            perfil.PiorSequenciaDeReds,
            perfil.Recomendacoes);
    }

    private sealed record PerfilParametros(Guid BancaId, Guid UsuarioId);
}
