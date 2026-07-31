using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ATT.Monitor.Api.Infrastructure.Files;

public sealed class MonitorFilePaths(IOptionsMonitor<MonitorFilePathsOptions> options) : IMonitorFilePaths
{
    public string GetArchivosAtmRoot() => options.CurrentValue.ArchivosAtmPath?.Trim() ?? string.Empty;

    public string GetJournalDiaRoot() => options.CurrentValue.JournalDiaPath?.Trim() ?? string.Empty;
}
