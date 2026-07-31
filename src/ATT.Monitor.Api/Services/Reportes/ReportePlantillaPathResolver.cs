using ATT.Monitor.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ATT.Monitor.Api.Services.Reportes;

public sealed class ReportePlantillaPathResolver(
    IWebHostEnvironment environment,
    IOptionsMonitor<ReportePlantillasOptions> options,
    ILogger<ReportePlantillaPathResolver> logger)
{
    public string? TryResolveTemplatePath(int idReporte)
    {
        if (!ReportePlantillaRegistry.TryGetTemplateFileName(idReporte, out var fileName))
            return null;

        var configured = options.CurrentValue.TemplatesDirectory;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var fromConfig = Path.Combine(configured.Trim(), fileName);
            if (File.Exists(fromConfig))
                return fromConfig;

            logger.LogWarning("Plantilla no encontrada en ReportePlantillas:TemplatesDirectory: {Path}", fromConfig);
        }

        var bundled = Path.Combine(environment.ContentRootPath, "Templates", "Reportes", fileName);
        return File.Exists(bundled) ? bundled : null;
    }
}
