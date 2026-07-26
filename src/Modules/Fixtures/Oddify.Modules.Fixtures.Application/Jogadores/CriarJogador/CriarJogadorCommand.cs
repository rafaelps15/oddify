using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Jogadores.CriarJogador;

public sealed record CriarJogadorCommand(string IdExterno, Guid EquipeId, string Nome, string Posicao) : ICommand<Guid>;
