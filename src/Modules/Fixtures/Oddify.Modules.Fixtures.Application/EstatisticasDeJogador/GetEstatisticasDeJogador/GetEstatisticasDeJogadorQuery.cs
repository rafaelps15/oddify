using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Fixtures.Application.EstatisticasDeJogador.GetEstatisticasDeJogador;

public sealed record GetEstatisticasDeJogadorQuery(Guid PartidaId) : IQuery<IReadOnlyCollection<EstatisticaJogadorResponse>>;
