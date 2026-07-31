using System.Globalization;
using System.IO.Compression;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Configuration;
using ATT.Monitor.Api.Infrastructure.Composition;
using ATT.Monitor.Api.Infrastructure.DetalleEquipo;
using ATT.Monitor.Api.Infrastructure.Persistence;
using ATT.Monitor.Api.Services;
using ATT.Monitor.Api.Services.Reportes;
using ATT.Monitor.Api.Infrastructure.Files;
using ATT.Monitor.Api.Infrastructure.Jobs;
using ATT.Monitor.Api.Security;
using ATT.Monitor.Api.Infrastructure.ExceptionHandling;
using ATT.Monitor.Api.Infrastructure.HealthChecks;
using ATT.Monitor.Api.Infrastructure.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.IIS;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ATT.Monitor.Api")
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "ATT.Monitor.Api")
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
    });

    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddAttSwaggerWithJwt();
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
    builder.Services.PostConfigure<JwtSettings>(static jwt => JwtSettings.ApplyEnvironmentDefaults(jwt));
    builder.Services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
    builder.Services.AddScoped<IAtmAccountData, AtmAccountDataService>();
    builder.Services.AddScoped<IAtmOperationsData, AtmOperationsDataService>();
    builder.Services.AddScoped<IDashboardData, DashboardDataService>();
    builder.Services.AddSingleton<IDetalleEquipoSoSyncCoordinator, DetalleEquipoSoSyncCoordinator>();
    builder.Services.Configure<ReporteHistoricoDescargaOptions>(
        builder.Configuration.GetSection(ReporteHistoricoDescargaOptions.SectionName));
    builder.Services.Configure<ReportePlantillasOptions>(
        builder.Configuration.GetSection(ReportePlantillasOptions.SectionName));
    builder.Services.AddScoped<ReportePlantillaPathResolver>();
    builder.Services.AddScoped<IReporteHistoricoDescargaService, ReporteHistoricoDescargaService>();
    builder.Services.AddScoped<IReporteGeneracionService, ReporteGeneracionService>();
    builder.Services.Configure<MonitorAgentOptions>(
        builder.Configuration.GetSection(MonitorAgentOptions.SectionName));
    builder.Services.Configure<MonitorFilePathsOptions>(
        builder.Configuration.GetSection(MonitorFilePathsOptions.SectionName));
    builder.Services.PostConfigure<MonitorFilePathsOptions>(static o => MonitorFilePathsOptions.ApplyEnvironmentDefaults(o));
    builder.Services.AddSingleton<IMonitorFilePaths, MonitorFilePaths>();
    builder.Services.Configure<JournalHistoricoOptions>(
        builder.Configuration.GetSection(JournalHistoricoOptions.SectionName));
    builder.Services.AddScoped<IJournalHistoricoService, JournalHistoricoService>();
    builder.Services.AddSingleton(JournalHistoricoExportChannel.Create());
    builder.Services.AddSingleton<JournalHistoricoExportJobStore>();
    builder.Services.AddSingleton<IJournalHistoricoExportJobStore>(static sp => sp.GetRequiredService<JournalHistoricoExportJobStore>());
    builder.Services.AddHostedService<JournalHistoricoExportWorker>();
    builder.Services.AddHostedService<JournalHistoricoExportMaintenanceHostedService>();
    builder.Services.AddScoped<ITransArchivoData, TransArchivoDataService>();
    builder.Services.AddScoped<ICampanaData, CampanaDataService>();
    builder.Services.AddScoped<IAdministradorData, AdministradorDataService>();
    builder.Services.AddScoped<ICatalogosMonitorData, CatalogosMonitorDataService>();
    builder.Services.AddScoped<ICampanaDocumentService, CampanaDocumentService>();
    builder.Services.AddScoped<IConciliacion, ConciliacionDataService>();

    builder.Services.Configure<MonitorCryptoOptions>(
        builder.Configuration.GetSection(MonitorCryptoOptions.SectionName));
    builder.Services.PostConfigure<MonitorCryptoOptions>(static o => MonitorCryptoOptions.ApplyEnvironmentDefaults(o));
    builder.Services.AddSingleton<ICrypto, MonitorAesCrypto>();
    builder.AddAttMonitoringSecurity();

    builder.Services.Configure<RequestBodyLimitsOptions>(
        builder.Configuration.GetSection(RequestBodyLimitsOptions.SectionName));
    builder.Services.PostConfigure<RequestBodyLimitsOptions>(static o => RequestBodyLimitsOptions.ApplyEnvironmentDefaults(o));

    var bodyLimits = builder.Configuration.GetSection(RequestBodyLimitsOptions.SectionName).Get<RequestBodyLimitsOptions>()
                     ?? new RequestBodyLimitsOptions();
    RequestBodyLimitsOptions.ApplyEnvironmentDefaults(bodyLimits);
    var maxRequestBodyBytes = bodyLimits.ResolveMaxBytes();
    Log.Information(
        "Límite de cuerpo HTTP (multipart): {Megabytes} MB ({Bytes} bytes). Variable MAX_REQUEST_BODY_MB si aplica.",
        Math.Clamp(bodyLimits.MaxMegabytes, RequestBodyLimitsOptions.MinMegabytes, RequestBodyLimitsOptions.MaxMegabytesCap),
        maxRequestBodyBytes);

    builder.Services.Configure<IISServerOptions>(options =>
    {
        options.MaxRequestBodySize = maxRequestBodyBytes;
    });
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = maxRequestBodyBytes;
    });
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = maxRequestBodyBytes;
    });

    builder.Services
        .AddHealthChecks()
        .AddCheck<LivenessHealthCheck>("live", tags: HealthCheckTags.Live)
        .AddCheck<SqlConnectionHealthCheck>("sqlserver", tags: HealthCheckTags.Ready);

    builder.Services.AddAuthorization();

    var app = builder.Build();

    app.UseForwardedHeaders();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseResponseCompression();

    if (!app.Environment.IsDevelopment())
        app.UseHsts();

    if (app.Configuration.GetValue("Swagger:Enabled", true))
    {
        var swaggerRoutePrefix = app.Configuration["Swagger:RoutePrefix"];
        if (string.IsNullOrWhiteSpace(swaggerRoutePrefix))
            swaggerRoutePrefix = "swagger";

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "ATT Monitor v1");
            c.RoutePrefix = swaggerRoutePrefix.Trim().Trim('/');
        });
    }

    app.UseMiddleware<CorrelationIdMiddleware>();
    // WebApplicationFactory + HttpClient HTTP: evitar 307 a https://localhost sin certificado de confianza.
    if (!string.Equals(
            app.Configuration["TestHost:DisableHttpsRedirection"],
            "true",
            StringComparison.OrdinalIgnoreCase))
    {
        app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains(HealthCheckTags.LiveName)
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains(HealthCheckTags.ReadyName)
    });
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true
    });

    app.MapControllers();

    await app.RunAsync().ConfigureAwait(false);
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

// Expone el ensamblado a Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TEntryPoint>.
public partial class Program { }
