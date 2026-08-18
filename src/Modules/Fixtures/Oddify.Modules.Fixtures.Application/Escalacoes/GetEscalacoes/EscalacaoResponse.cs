namespace Oddify.Modules.Fixtures.Application.Escalacoes.GetEscalacoes;

public sealed record EscalacaoResponse(Guid Id, Guid PartidaId, Guid EquipeId, string Formacao, string Tecnico)
{
    // Fora do construtor posicional — populada pelo callback do multi-mapping no handler, nunca por
    // um Row intermediário nem por um .ToResponse(...) depois de montar a lista (query-slice.md §B4).
    public List<EscalacaoJogadorResponse> Jogadores { get; } = [];
}

// EscalacaoJogadorId, não Id — evita colisão de alias com EscalacaoResponse.Id no splitOn do
// multi-mapping (as duas colunas teriam o mesmo alias "Id" senão).
public sealed record EscalacaoJogadorResponse(Guid EscalacaoJogadorId, Guid JogadorId, bool Titular, string Posicao, int? Numero);
