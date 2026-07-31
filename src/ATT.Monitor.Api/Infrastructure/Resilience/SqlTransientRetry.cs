using Microsoft.Data.SqlClient;
using Polly;

namespace ATT.Monitor.Api.Infrastructure.Resilience;

/// <summary>
/// Reintentos acotados ante fallos transitorios de SQL (timeouts, deadlock, throttling Azure SQL).
/// Paridad con API v1 (<c>BC_MonitorAPIs.Infrastructure.SqlTransientRetry</c>).
/// </summary>
public static class SqlTransientRetry
{
    private static readonly IAsyncPolicy DefaultPolicy = Policy
        .Handle<SqlException>(IsTransientSql)
        .WaitAndRetryAsync(
            retryCount: 2,
            sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(80 * attempt));

    public static Task<T> ExecuteAsync<T>(Func<Task<T>> action) => DefaultPolicy.ExecuteAsync(action);

    private static bool IsTransientSql(SqlException ex) =>
        ex.Number is -2 or 1205 or 4060 or 40197 or 40501 or 49918 or 64 or 233;
}
