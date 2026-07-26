namespace Oddify.Modules.Fixtures.Application.Jogadores.GetJogador;

public sealed record JogadorResponse(Guid Id, string IdExterno, Guid EquipeId, string Nome, string Posicao);
