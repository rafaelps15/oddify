using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Authentication;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;
using Oddify.Modules.Apostas.Application.Calculo;
using Oddify.Modules.Apostas.Domain.JornadasDeAlavancagem;

namespace Oddify.Modules.Apostas.Application.JornadasDeAlavancagem.GetJornada;

internal sealed class GetJornadaQueryHandler(IDbConnectionFactory dbConnectionFactory, IUserContext userContext)
    : IQueryHandler<GetJornadaQuery, JornadaResponse>
{
    public async Task<Result<JornadaResponse>> Handle(GetJornadaQuery request, CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // ValorAtual vem de bancas.saldo_atual (a banca de alavancagem está sempre 100% alocada no
        // passo corrente, ver comentário em AvaliarPassoDaJornadaCommandHandler), não de um campo
        // próprio da jornada.
        const string sqlJornada =
            $"""
             SELECT
                 j.id AS {nameof(JornadaHeaderRow.Id)},
                 j.faixa_meta AS {nameof(JornadaHeaderRow.FaixaMeta)},
                 j.passo_atual AS {nameof(JornadaHeaderRow.PassoAtual)},
                 j.total_de_passos AS {nameof(JornadaHeaderRow.TotalDePassos)},
                 j.numero_de_fracoes AS {nameof(JornadaHeaderRow.NumeroDeFracoes)},
                 j.valor_inicial AS {nameof(JornadaHeaderRow.ValorInicial)},
                 j.valor_objetivo AS {nameof(JornadaHeaderRow.ValorObjetivo)},
                 j.status AS {nameof(JornadaHeaderRow.Status)},
                 j.probabilidade_de_conclusao AS {nameof(JornadaHeaderRow.ProbabilidadeDeConclusao)},
                 b.saldo_atual AS {nameof(JornadaHeaderRow.ValorAtual)}
             FROM apostas.jornadas_de_alavancagem j
             JOIN apostas.bancas b ON b.id = j.banca_id
             WHERE j.id = @JornadaId AND j.usuario_id = @UserId
             """;

        JornadaHeaderRow? header = await connection.QuerySingleOrDefaultAsync<JornadaHeaderRow>(
            sqlJornada, new { request.JornadaId, userContext.UserId });

        if (header is null)
        {
            return Result.Failure<JornadaResponse>(JornadaDeAlavancagemErrors.NotFound(request.JornadaId));
        }

        // As apostas do passo mais recente (qualquer status — Montado/EmAberto/Avancou/Quebrou),
        // não só o EmAberto: depois que a jornada conclui ou quebra não há mais passo EmAberto,
        // mas a tela ainda quer mostrar como foi o último passo.
        const string sqlApostas =
            $"""
             SELECT
                 a.id AS {nameof(ApostaDoPassoAtualResponse.Id)},
                 a.descricao AS {nameof(ApostaDoPassoAtualResponse.Descricao)},
                 a.odd_combinada AS {nameof(ApostaDoPassoAtualResponse.OddCombinada)},
                 a.stake AS {nameof(ApostaDoPassoAtualResponse.Stake)},
                 a.resultado AS {nameof(ApostaDoPassoAtualResponse.Resultado)},
                 a.lucro_ou_perda AS {nameof(ApostaDoPassoAtualResponse.LucroOuPerda)}
             FROM apostas.apostas_multiplas a
             WHERE a.passo_da_jornada_id = (
                 SELECT pj.id
                 FROM apostas.passos_da_jornada pj
                 WHERE pj.jornada_id = @JornadaId
                 ORDER BY pj.criado_em_utc DESC
                 LIMIT 1
             )
             """;

        List<ApostaDoPassoAtualResponse> apostasDoPassoAtual =
            (await connection.QueryAsync<ApostaDoPassoAtualResponse>(sqlApostas, new { request.JornadaId })).AsList();

        decimal bancaMinima = RegrasDeAlavancagem.CalcularBancaMinima(header.NumeroDeFracoes);

        return new JornadaResponse(
            header.Id,
            header.FaixaMeta,
            header.PassoAtual,
            header.TotalDePassos,
            header.NumeroDeFracoes,
            header.ValorInicial,
            header.ValorAtual,
            header.ValorObjetivo,
            bancaMinima,
            header.Status,
            header.ProbabilidadeDeConclusao,
            apostasDoPassoAtual);
    }
}
