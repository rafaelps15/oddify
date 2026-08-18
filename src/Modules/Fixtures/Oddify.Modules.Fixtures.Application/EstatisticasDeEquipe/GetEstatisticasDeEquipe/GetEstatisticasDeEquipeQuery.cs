using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.EstatisticasDeEquipe.GetEstatisticasDeEquipe;

public sealed record GetEstatisticasDeEquipeQuery(Guid PartidaId) : IQuery<IReadOnlyCollection<EstatisticaEquipeResponse>>;
