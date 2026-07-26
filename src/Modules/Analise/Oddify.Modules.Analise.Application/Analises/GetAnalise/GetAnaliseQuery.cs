using Oddify.Common.Application.Messaging;

namespace Oddify.Modules.Analise.Application.Analises.GetAnalise;

public sealed record GetAnaliseQuery(Guid AnaliseId) : IQuery<AnaliseResponse>;
