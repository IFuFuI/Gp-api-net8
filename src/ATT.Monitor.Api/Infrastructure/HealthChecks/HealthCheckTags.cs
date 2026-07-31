namespace ATT.Monitor.Api.Infrastructure.HealthChecks;

internal static class HealthCheckTags
{
    public const string LiveName = "live";
    public const string ReadyName = "ready";

    public static readonly string[] Live = [LiveName];
    public static readonly string[] Ready = [ReadyName];
}
