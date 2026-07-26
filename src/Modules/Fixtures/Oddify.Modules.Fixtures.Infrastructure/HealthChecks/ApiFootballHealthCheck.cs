using Microsoft.Extensions.Diagnostics.HealthChecks;
using Oddify.Modules.Fixtures.Application.Abstractions.ExternalData;

namespace Oddify.Modules.Fixtures.Infrastructure.HealthChecks;

internal sealed class ApiFootballHealthCheck(IApiFootballClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        bool disponivel = await client.VerificarStatusAsync(cancellationToken);

        return disponivel
            ? HealthCheckResult.Healthy("API-Football respondendo")
            : HealthCheckResult.Unhealthy("API-Football não respondeu");
    }
}
