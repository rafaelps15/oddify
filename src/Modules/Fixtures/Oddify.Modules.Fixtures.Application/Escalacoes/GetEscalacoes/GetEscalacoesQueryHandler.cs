using System.Data.Common;
using Dapper;
using Oddify.Common.Application.Data;
using Oddify.Common.Application.Messaging;
using Oddify.Common.Domain;

namespace Oddify.Modules.Fixtures.Application.Escalacoes.GetEscalacoes;

internal sealed class GetEscalacoesQueryHandler(IDbConnectionFactory dbConnectionFactory)
    : IQueryHandler<GetEscalacoesQuery, IReadOnlyCollection<EscalacaoResponse>>
{
    public async Task<Result<IReadOnlyCollection<EscalacaoResponse>>> Handle(
        GetEscalacoesQuery request,
        CancellationToken cancellationToken)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();

        // LEFT JOIN: uma escalação registrada sem nenhum jogador ainda (POST de equipe feito, POST
        // de jogador ainda não) continua aparecendo, só com Jogadores vazio — nunca um erro.
        const string sql =
            $"""
             SELECT
                 e.id AS {nameof(EscalacaoResponse.Id)},
                 e.partida_id AS {nameof(EscalacaoResponse.PartidaId)},
                 e.equipe_id AS {nameof(EscalacaoResponse.EquipeId)},
                 e.formacao AS {nameof(EscalacaoResponse.Formacao)},
                 e.tecnico AS {nameof(EscalacaoResponse.Tecnico)},
                 ej.id AS {nameof(EscalacaoJogadorResponse.EscalacaoJogadorId)},
                 ej.jogador_id AS {nameof(EscalacaoJogadorResponse.JogadorId)},
                 ej.titular AS {nameof(EscalacaoJogadorResponse.Titular)},
                 ej.posicao AS {nameof(EscalacaoJogadorResponse.Posicao)},
                 ej.numero AS {nameof(EscalacaoJogadorResponse.Numero)}
             FROM fixtures.escalacoes e
             LEFT JOIN fixtures.escalacoes_de_jogador ej ON ej.escalacao_id = e.id
             WHERE e.partida_id = @PartidaId
             """;

        List<EscalacaoResponse> escalacoes = [];
        var escalacoesPorId = new Dictionary<Guid, EscalacaoResponse>();

        await connection.QueryAsync<EscalacaoResponse, EscalacaoJogadorResponse?, EscalacaoResponse>(
            sql,
            (escalacao, jogador) =>
            {
                if (!escalacoesPorId.TryGetValue(escalacao.Id, out EscalacaoResponse? existente))
                {
                    existente = escalacao;
                    escalacoesPorId.Add(existente.Id, existente);
                    escalacoes.Add(existente);
                }

                if (jogador is not null)
                {
                    existente.Jogadores.Add(jogador);
                }

                return existente;
            },
            request,
            splitOn: nameof(EscalacaoJogadorResponse.EscalacaoJogadorId));

        return escalacoes;
    }
}
