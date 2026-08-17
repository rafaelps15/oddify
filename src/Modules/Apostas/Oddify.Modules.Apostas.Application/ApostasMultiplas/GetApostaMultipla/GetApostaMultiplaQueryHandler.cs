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
             WHERE am.id = @ApostaMultiplaId
             """;

        Dictionary<Guid, ApostaMultiplaResponse> apostasMultiplasDictionary = [];

        await connection.QueryAsync<ApostaMultiplaResponse, PernaResponse?, ApostaMultiplaResponse>(
            sql,
            (aposta, perna) =>
            {
                if (apostasMultiplasDictionary.TryGetValue(aposta.Id, out ApostaMultiplaResponse? existingApostaMultipla))
                {
                    aposta = existingApostaMultipla;
                }
                else
                {
                    apostasMultiplasDictionary.Add(aposta.Id, aposta);
                }

                if (perna is not null)
                {
                    aposta.Pernas.Add(perna);
                }

                return aposta;
            },
            request,
            splitOn: nameof(PernaResponse.PernaId));

        if (!apostasMultiplasDictionary.TryGetValue(request.ApostaMultiplaId, out ApostaMultiplaResponse? apostaMultiplaResponse))
        {
            return Result.Failure<ApostaMultiplaResponse>(ApostaMultiplaErrors.NotFound(request.ApostaMultiplaId));
        }

        IReadOnlyCollection<PartidaResumoResponse> partidas = await fixturesApi.ObterPartidasResumoAsync(
            apostaMultiplaResponse.Pernas.Select(p => p.PartidaId).Distinct().ToList(), cancellationToken);

        var partidasPorId = partidas.ToDictionary(p => p.Id);

        apostaMultiplaResponse.Pernas.ForEach(perna => perna.Enriquecer(partidasPorId.GetValueOrDefault(perna.PartidaId)));

        return apostaMultiplaResponse;
    }
}
