using System.Diagnostics;
using Serilog.Context;

namespace ATT.Monitor.Api.Infrastructure.Middleware;

/// <summary>
/// Propaga <c>X-Correlation-ID</c> y lo expone en <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/> para logs y ProblemDetails.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string HttpContextItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Items[HttpContextItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
            await _next(context).ConfigureAwait(false);
    }
}
