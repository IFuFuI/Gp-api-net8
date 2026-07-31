using System.Diagnostics;
using ATT.Monitor.Api.Infrastructure.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Infrastructure.ExceptionHandling;

/// <summary>
/// Convierte excepciones no controladas en <see cref="ProblemDetails"/> (RFC 7807, application/problem+json).
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var correlationId = httpContext.Items[CorrelationIdMiddleware.HttpContextItemKey] as string ?? traceId;

        logger.LogError(exception, "Error no controlado. CorrelationId: {CorrelationId}", correlationId);

        var statusCode = StatusCodes.Status500InternalServerError;
        var detail = environment.IsDevelopment()
            ? exception.Message
            : "Ocurrió un error interno. Use el identificador de seguimiento para soporte.";

        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = "Error interno del servidor",
            Status = statusCode,
            Detail = detail,
            Instance = httpContext.Request.Path.Value
        };
        problem.Extensions["traceId"] = traceId;
        problem.Extensions["correlationId"] = correlationId;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response
            .WriteAsJsonAsync(problem, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return true;
    }
}
