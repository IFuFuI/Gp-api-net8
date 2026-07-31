using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ATT.Monitor.Api.Infrastructure.HealthChecks;

/// <summary>
/// Liveness: el proceso responde (sin comprobar SQL u otros servicios).
/// </summary>
public sealed class LivenessHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
        => Task.FromResult(HealthCheckResult.Healthy("Proceso activo."));
}
