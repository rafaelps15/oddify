using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetResultadosDasPernas;

public sealed record GetResultadosDasPernasQuery(IReadOnlyCollection<PernaParaResolver> Pernas)
    : IQuery<IReadOnlyDictionary<Guid, bool>>;

public sealed record PernaParaResolver(Guid PernaId, Guid PartidaId, string Mercado);
