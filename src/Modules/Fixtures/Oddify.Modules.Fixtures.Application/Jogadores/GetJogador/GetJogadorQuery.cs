using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Jogadores.GetJogador;

public sealed record GetJogadorQuery(Guid JogadorId) : IQuery<JogadorResponse>;
