using ATT.Monitor.Api.Abstractions;

namespace ATT.Monitor.Api.Infrastructure.Jobs;

/// <summary>Consume la cola y escribe cada ZIP mediante <see cref="IJournalHistoricoService"/> (scoped).</summary>
public sealed class JournalHistoricoExportWorker(
    JournalHistoricoExportChannel channel,
    JournalHistoricoExportJobStore store,
    ILogger<JournalHistoricoExportWorker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in channel.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            await ProcessSingleJobAsync(jobId, stoppingToken).ConfigureAwait(false);
    }

    private async Task ProcessSingleJobAsync(string jobId, CancellationToken ct)
    {
        if (!store.BeginProcessingJob(jobId, out var request) || request is null)
            return;

        var tempPath = Path.Combine(Path.GetTempPath(), $"journal-job-{jobId}.zip");

        try
        {
            using var scope = scopeFactory.CreateScope();
            var journal = scope.ServiceProvider.GetRequiredService<IJournalHistoricoService>();
            var (_, summary) = await journal.WriteExportZipToFileAsync(request, tempPath, ct).ConfigureAwait(false);
            summary.NombreZip = string.IsNullOrWhiteSpace(summary.NombreZip) ? $"journal-{jobId}.zip" : summary.NombreZip;
            store.CompleteJobSuccess(jobId, tempPath, summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo job export journal {JobId}", jobId);
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                /* best effort */
            }

            store.FailJob(jobId, ex.Message.Length > 0 ? ex.Message : "No se pudo generar el ZIP.");
        }
    }
}
