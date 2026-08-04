using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Apostas.Application.ApostasMultiplas.GetAnalisesDisponiveisParaAposta;

public sealed record GetAnalisesDisponiveisParaApostaQuery : IQuery<IReadOnlyCollection<AnaliseDisponivelParaApostaResponse>>;
