using Microsoft.Extensions.Diagnostics.HealthChecks;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;

namespace Oddify.Modules.Fixtures.Infrastructure.HealthChecks;

internal sealed class TheOddsApiHealthCheck(ITheOddsApiClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        bool disponivel = await client.VerificarStatusAsync(cancellationToken);

        return disponivel
            ? HealthCheckResult.Healthy("The Odds API respondendo")
            : HealthCheckResult.Unhealthy("The Odds API não respondeu");
    }
}
