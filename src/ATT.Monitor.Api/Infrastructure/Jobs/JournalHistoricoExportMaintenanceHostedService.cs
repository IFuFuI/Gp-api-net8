using ATT.Monitor.Api.Abstractions;

namespace ATT.Monitor.Api.Infrastructure.Jobs;

/// <summary>Limpieza periódica de jobs/expiraciones en memoria.</summary>
public sealed class JournalHistoricoExportMaintenanceHostedService(
    IJournalHistoricoExportJobStore store,
    ILogger<JournalHistoricoExportMaintenanceHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        try
        {
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    store.CleanupExpiredJobs();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Limpieza de jobs journal export.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            /* shutdown */
        }
    }
}
