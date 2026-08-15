using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;
using Oddify.Modules.Fixtures.PublicApi;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostasMultiplas;

internal sealed class GetApostasMultiplasQueryHandler(IDbConnectionFactory dbConnectionFactory, IFixturesApi fixturesApi)
    : IQueryHandler<GetApostasMultiplasQuery, IReadOnlyCollection<ApostaMultiplaResponse>>
{
    public async Task<Result<IReadOnlyCollection<ApostaMultiplaResponse>>> Handle(
        GetApostasMultiplasQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 am.id AS {nameof(ApostaMultiplaResponse.Id)},
                 am.banca_id AS {nameof(ApostaMultiplaResponse.BancaId)},
                 am.odd_combinada AS {nameof(ApostaMultiplaResponse.OddCombinada)},
                 am.stake AS {nameof(ApostaMultiplaResponse.Stake)},
                 am.resultado AS {nameof(ApostaMultiplaResponse.Resultado)},
                 am.lucro_ou_perda AS {nameof(ApostaMultiplaResponse.LucroOuPerda)},
                 am.criada_em_utc AS {nameof(ApostaMultiplaResponse.CriadaEmUtc)},
                 p.id AS {nameof(PernaResponse.PernaId)},
                 p.mercado AS {nameof(PernaResponse.Mercado)},
                 p.odd AS {nameof(PernaResponse.Odd)},
                 p.partida_id AS {nameof(PernaResponse.PartidaId)},
                 p.resultado AS {nameof(PernaResponse.Resultado)}
             FROM apostas.apostas_multiplas am
             LEFT JOIN apostas.pernas_de_aposta p ON p.aposta_multipla_id = am.id
             WHERE @BancaId IS NULL OR am.banca_id = @BancaId
             ORDER BY am.criada_em_utc DESC
             """;

        List<ApostaMultiplaResponse> apostas = [];
        var apostasPorId = new Dictionary<Guid, ApostaMultiplaResponse>();

        await connection.QueryAsync<ApostaMultiplaResponse, PernaResponse?, ApostaMultiplaResponse>(
            sql,
            (aposta, perna) =>
            {
                if (!apostasPorId.TryGetValue(aposta.Id, out ApostaMultiplaResponse? existente))
                {
                    existente = aposta;
                    apostasPorId.Add(existente.Id, existente);
                    apostas.Add(existente);
                }

                if (perna is not null)
                {
                    existente.Pernas.Add(perna);
                }

                return existente;
            },
            request,
            splitOn: nameof(PernaResponse.PernaId));

        IReadOnlyCollection<Guid> partidaIds = apostas.SelectMany(a => a.Pernas).Select(p => p.PartidaId).Distinct().ToList();
        IReadOnlyCollection<PartidaResumoResponse> partidas = await fixturesApi.ObterPartidasResumoAsync(partidaIds, cancellationToken);
        var partidasPorId = partidas.ToDictionary(p => p.Id);

        apostas.SelectMany(a => a.Pernas)
            .ToList()
            .ForEach(perna => perna.Enriquecer(partidasPorId.GetValueOrDefault(perna.PartidaId)));

        return apostas;
    }
}
