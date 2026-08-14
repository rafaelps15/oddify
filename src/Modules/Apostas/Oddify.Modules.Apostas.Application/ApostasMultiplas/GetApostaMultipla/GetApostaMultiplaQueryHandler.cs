using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Domain.ApostasMultiplas;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;

internal sealed class GetApostaMultiplaQueryHandler(IDbConnectionFactory dbConnectionFactory, IFixturesApi fixturesApi)
    : IQueryHandler<GetApostaMultiplaQuery, ApostaMultiplaResponse>
{
    public async Task<Result<ApostaMultiplaResponse>> Handle(GetApostaMultiplaQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 id AS {nameof(ApostaMultiplaRow.Id)},
                 banca_id AS {nameof(ApostaMultiplaRow.BancaId)},
                 odd_combinada AS {nameof(ApostaMultiplaRow.OddCombinada)},
                 stake AS {nameof(ApostaMultiplaRow.Stake)},
                 resultado AS {nameof(ApostaMultiplaRow.Resultado)},
                 lucro_ou_perda AS {nameof(ApostaMultiplaRow.LucroOuPerda)},
                 criada_em_utc AS {nameof(ApostaMultiplaRow.CriadaEmUtc)}
             FROM apostas.apostas_multiplas
             WHERE id = @ApostaMultiplaId
             """;

        ApostaMultiplaRow? row = await connection.QuerySingleOrDefaultAsync<ApostaMultiplaRow>(sql, request);

        if (row is null)
        {
            return Result.Failure<ApostaMultiplaResponse>(ApostaMultiplaErrors.NotFound(request.ApostaMultiplaId));
        }

        IReadOnlyCollection<PernaResponse> pernas = await GetPernasAsync(connection, row.Id, cancellationToken);

        return row.ToResponse(pernas);
    }

    private async Task<IReadOnlyCollection<PernaResponse>> GetPernasAsync(
        DbConnection connection,
        Guid apostaMultiplaId,
        CancellationToken cancellationToken)
    {
        const string sql =
            $"""
             SELECT
                 id AS {nameof(PernaRow.Id)},
                 mercado AS {nameof(PernaRow.Mercado)},
                 odd AS {nameof(PernaRow.Odd)},
                 partida_id AS {nameof(PernaRow.PartidaId)},
                 resultado AS {nameof(PernaRow.Resultado)}
             FROM apostas.pernas_de_aposta
             WHERE aposta_multipla_id = @ApostaMultiplaId
             """;

        List<PernaRow> rows = (await connection.QueryAsync<PernaRow>(sql, new { ApostaMultiplaId = apostaMultiplaId })).AsList();

        IReadOnlyCollection<PartidaResumoResponse> partidas =
            await fixturesApi.ObterPartidasResumoAsync(rows.Select(r => r.PartidaId).Distinct().ToList(), cancellationToken);

        var partidasPorId = partidas.ToDictionary(p => p.Id);

        return rows.Select(r => r.ToResponse(partidasPorId.GetValueOrDefault(r.PartidaId))).ToList();
    }
}
