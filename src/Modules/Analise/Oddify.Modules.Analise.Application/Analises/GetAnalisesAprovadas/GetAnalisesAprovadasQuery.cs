using Oddify.Common.Application.Messaging;
using Oddify.Modules.Analise.Application.Analises.GetAnalise;

namespace Oddify.Modules.Analise.Application.Analises.GetAnalisesAprovadas;

public sealed record GetAnalisesAprovadasQuery : IQuery<IReadOnlyCollection<AnaliseResponse>>;
