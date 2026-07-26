using Oddify.Common.Application.Messaging;
using Oddify.Modules.Fixtures.Application.Jogadores.GetJogador;

namespace Oddify.Modules.Fixtures.Application.Jogadores.GetJogadores;

public sealed record GetJogadoresQuery(Guid EquipeId) : IQuery<IReadOnlyCollection<JogadorResponse>>;
