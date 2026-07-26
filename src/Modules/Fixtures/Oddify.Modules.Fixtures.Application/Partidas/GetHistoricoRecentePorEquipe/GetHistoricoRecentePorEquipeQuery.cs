using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetHistoricoRecentePorEquipe;

public sealed record GetHistoricoRecentePorEquipeQuery(Guid EquipeId, int MinimoDeJogos) : IQuery<HistoricoDeEquipeResponse>;
