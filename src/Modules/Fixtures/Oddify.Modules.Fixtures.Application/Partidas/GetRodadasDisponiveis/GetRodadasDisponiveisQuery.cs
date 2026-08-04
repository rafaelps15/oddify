using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.Partidas.GetRodadasDisponiveis;

public sealed record GetRodadasDisponiveisQuery(Guid? LigaId, int Temporada) : IQuery<IReadOnlyCollection<int>>;
