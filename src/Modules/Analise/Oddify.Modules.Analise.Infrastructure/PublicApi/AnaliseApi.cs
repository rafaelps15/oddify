using Oddify.Modules.Analise.Domain.Analises;
using Oddify.Modules.Analise.PublicApi;

namespace Oddify.Modules.Analise.Infrastructure.PublicApi;

internal sealed class AnaliseApi : IAnaliseApi
{
    public bool ResolverMercado(string mercado, int golsCasa, int golsVisitante) =>
        MercadoResolver.Resolver(mercado, golsCasa, golsVisitante);
}
