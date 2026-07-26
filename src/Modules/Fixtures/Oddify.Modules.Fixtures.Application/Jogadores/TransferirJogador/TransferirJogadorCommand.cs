using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Jogadores.TransferirJogador;

public sealed record TransferirJogadorCommand(Guid JogadorId, Guid NovaEquipeId) : ICommand;
