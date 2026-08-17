using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Calculo;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetFaixasDeMeta;

internal sealed class GetFaixasDeMetaQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetFaixasDeMetaQuery, IReadOnlyCollection<FaixaDeMetaResponse>>
{
    public async Task<Result<IReadOnlyCollection<FaixaDeMetaResponse>>> Handle(
        GetFaixasDeMetaQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        const string sql =
            $"""
             SELECT
                 faixa AS {nameof(FaixaDeMetaResponse.Faixa)},
                 multiplicador AS {nameof(FaixaDeMetaResponse.Multiplicador)},
                 numero_de_fracoes AS {nameof(FaixaDeMetaResponse.NumeroDeFracoes)},
                 total_de_passos AS {nameof(FaixaDeMetaResponse.TotalDePassos)}
             FROM apostas.faixas_de_meta_catalogo
             """;

        List<FaixaDeMetaResponse> faixas = (await connection.QueryAsync<FaixaDeMetaResponse>(sql)).AsList();

        faixas.ForEach(f => f.DefinirProbabilidadeDeConclusao(
            RegrasDeAlavancagem.CalcularProbabilidadeDeConclusao(f.NumeroDeFracoes, f.TotalDePassos)));

        return faixas;
    }
}
