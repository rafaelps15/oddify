using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetApostaMultipla;

public sealed record GetApostaMultiplaQuery(Guid ApostaMultiplaId) : IQuery<ApostaMultiplaResponse>;
