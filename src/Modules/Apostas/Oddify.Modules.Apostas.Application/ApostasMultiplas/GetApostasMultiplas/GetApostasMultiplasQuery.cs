using Oddify.Common.Application.Messaging;
using Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostasMultiplas;

public sealed record GetApostasMultiplasQuery(Guid? BancaId) : IQuery<IReadOnlyCollection<ApostaMultiplaResponse>>;
