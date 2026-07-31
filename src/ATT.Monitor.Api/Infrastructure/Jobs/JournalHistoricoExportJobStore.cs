using System.Collections.Concurrent;
using System.Text.Json;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Configuration;
using ATT.Monitor.Api.Models.JournalHistorico;
using Microsoft.Extensions.Options;

namespace ATT.Monitor.Api.Infrastructure.Jobs;

public sealed class JournalHistoricoExportJobStore(IOptionsMonitor<JournalHistoricoOptions> options) : IJournalHistoricoExportJobStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly ConcurrentDictionary<string, ExportJobInternal> _jobs = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _userToActiveJob = new(StringComparer.Ordinal);

    public bool TryCreateJob(string userId, JournalHistoricoDownloadRequest payload, out string jobId, out string? rejectionReason)
    {
        rejectionReason = null;
        jobId = string.Empty;

        if (string.IsNullOrWhiteSpace(userId))
        {
            rejectionReason = "Sin identidad de usuario.";
            return false;
        }

        if (_userToActiveJob.ContainsKey(userId))
        {
            rejectionReason = "Ya tiene una exportación en curso. Espere a que finalice antes de iniciar otra.";
            return false;
        }

        jobId = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
        var snap = ClonePayload(payload);
        var payloadJson = JsonSerializer.Serialize(snap, SerializerOptions);
        var job = new ExportJobInternal
        {
            Id = jobId,
            UserId = userId.Trim(),
            Status = JournalHistoricoExportJobStatus.Pending,
            CreatedUtc = DateTime.UtcNow,
            PayloadJson = payloadJson
        };

        if (!_jobs.TryAdd(jobId, job))
            return false;

        if (!_userToActiveJob.TryAdd(userId, jobId))
        {
            _jobs.TryRemove(jobId, out _);
            rejectionReason = "Ya tiene una exportación en curso. Espere a que finalice antes de iniciar otra.";
            return false;
        }

        return true;
    }

    public JournalHistoricoExportJobStatusDto? GetJob(string requestingUserId, string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job) ||
            !string.Equals(job.UserId, requestingUserId, StringComparison.Ordinal))
            return null;
        return ToDto(job);
    }

    public JournalHistoricoExportJobStatusDto? GetActiveJobForUser(string userId)
    {
        if (!_userToActiveJob.TryGetValue(userId, out var jobId))
            return null;

        _jobs.TryGetValue(jobId, out var job);
        if (job is null || job.Status >= JournalHistoricoExportJobStatus.Succeeded)
            return null;

        return ToDto(job);
    }

    public Task<(Stream? Content, string FileName)> OpenDownloadAsync(string requestingUserId, string jobId, CancellationToken ct)
    {
        _ = ct;
        if (!_jobs.TryGetValue(jobId, out var job) ||
            !string.Equals(job.UserId, requestingUserId, StringComparison.Ordinal))
            return Task.FromResult<(Stream?, string)>((null, string.Empty));

        if (job.Status != JournalHistoricoExportJobStatus.Succeeded ||
            string.IsNullOrWhiteSpace(job.TempZipPath) ||
            !File.Exists(job.TempZipPath))
            return Task.FromResult<(Stream?, string)>((null, string.Empty));

        var name = string.IsNullOrWhiteSpace(job.Summary?.NombreZip)
            ? "journal.zip"
            : job.Summary!.NombreZip;

        Stream stream = new FileStream(job.TempZipPath!, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.None);
        return Task.FromResult<(Stream?, string)>((stream, name));
    }

    public void CleanupExpiredJobs()
    {
        var minutes = Math.Max(15, options.CurrentValue.ExportJobRetentionMinutes);
        var cutoff = DateTime.UtcNow.AddMinutes(-minutes);
        var keys = _jobs.Keys.ToList();

        foreach (var jobId in keys)
        {
            if (!_jobs.TryGetValue(jobId, out var job))
                continue;
            if (job.FinishedUtc.HasValue && job.FinishedUtc < cutoff)
                RemoveFinishedJob(job.Id);
            else if (!job.FinishedUtc.HasValue && job.CreatedUtc < cutoff)
                FailJob(job.Id, job.ErrorMessage ?? "Expiró por tiempo (no finalizó).");
        }
    }

    internal sealed class ExportJobInternal
    {
        public required string Id { get; init; }
        public required string UserId { get; init; }
        public required string PayloadJson { get; init; }
        public JournalHistoricoExportJobStatus Status { get; set; }
        public DateTime CreatedUtc { get; init; }
        public DateTime? StartedUtc { get; set; }
        public DateTime? FinishedUtc { get; set; }
        public string? ErrorMessage { get; set; }
        public string? TempZipPath { get; set; }
        public JournalHistoricoDownloadSummaryDto? Summary { get; set; }

        internal readonly object Gate = new();
    }

    internal bool BeginProcessingJob(string jobId, out JournalHistoricoDownloadRequest? request)
    {
        request = null;
        if (!_jobs.TryGetValue(jobId, out var job))
            return false;

        JournalHistoricoDownloadRequest? deserialized = null;
        var deserializationFailed = false;
        lock (job.Gate)
        {
            if (job.Status != JournalHistoricoExportJobStatus.Pending)
                return false;
            job.Status = JournalHistoricoExportJobStatus.Processing;
            job.StartedUtc = DateTime.UtcNow;
            try
            {
                deserialized = JsonSerializer.Deserialize<JournalHistoricoDownloadRequest>(job.PayloadJson, SerializerOptions);
                if (deserialized is null)
                {
                    job.Status = JournalHistoricoExportJobStatus.Failed;
                    job.FinishedUtc = DateTime.UtcNow;
                    job.ErrorMessage = "No se pudo leer la solicitud del job.";
                    deserializationFailed = true;
                }
            }
            catch
            {
                job.Status = JournalHistoricoExportJobStatus.Failed;
                job.FinishedUtc = DateTime.UtcNow;
                job.ErrorMessage = "Solicitud de job inválida.";
                deserializationFailed = true;
            }

            if (deserializationFailed)
                _userToActiveJob.TryRemove(job.UserId, out _);
            else
                request = deserialized;
        }

        return !deserializationFailed && request is not null;
    }

    private static JournalHistoricoDownloadRequest ClonePayload(JournalHistoricoDownloadRequest r)
    {
        return new JournalHistoricoDownloadRequest
        {
            Ep = r.Ep,
            AllowedEps = r.AllowedEps?.ToArray(),
            Ids = r.Ids?.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray() ?? Array.Empty<string>(),
            Unificar = r.Unificar
        };
    }

    internal static JournalHistoricoExportJobStatusDto ToDto(ExportJobInternal job)
        => new()
        {
            JobId = job.Id,
            Status = job.Status,
            CreatedUtc = job.CreatedUtc,
            StartedUtc = job.StartedUtc,
            FinishedUtc = job.FinishedUtc,
            ErrorMessage = job.ErrorMessage,
            FileName = job.Summary?.NombreZip,
            DownloadReady = job.Status == JournalHistoricoExportJobStatus.Succeeded && File.Exists(job.TempZipPath ?? ""),
            Summary = job.Summary
        };

    internal void CompleteJobSuccess(string jobId, string zipPath, JournalHistoricoDownloadSummaryDto summary)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
            return;
        lock (job.Gate)
        {
            job.Status = JournalHistoricoExportJobStatus.Succeeded;
            job.FinishedUtc = DateTime.UtcNow;
            job.TempZipPath = zipPath;
            job.Summary = summary;
            job.ErrorMessage = null;
        }
        _userToActiveJob.TryRemove(job.UserId, out _);
    }

    internal void FailJob(string jobId, string error)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
            return;
        lock (job.Gate)
        {
            if (job.Status == JournalHistoricoExportJobStatus.Succeeded)
                return;

            DeleteTempFileQuiet(job.TempZipPath);
            job.TempZipPath = null;
            job.Status = JournalHistoricoExportJobStatus.Failed;
            job.FinishedUtc = DateTime.UtcNow;
            job.ErrorMessage = error;
        }

        _userToActiveJob.TryRemove(job.UserId, out _);
    }

    private void RemoveFinishedJob(string jobId)
    {
        if (!_jobs.TryRemove(jobId, out var job))
            return;
        DeleteTempFileQuiet(job.TempZipPath);
        _userToActiveJob.TryRemove(job.UserId, out _);
    }

    private static void DeleteTempFileQuiet(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            /* best effort */
        }
    }
}
