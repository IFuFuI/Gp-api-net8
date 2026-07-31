using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>
/// Información del servicio y comprobación básica (sin autenticación).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public sealed class SystemController(IHostEnvironment environment) : ControllerBase
{
    /// <summary>
    /// Versión y entorno de la API (útil para balanceadores y despliegues).
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(SystemInfoResponse), StatusCodes.Status200OK)]
    public ActionResult<SystemInfoResponse> GetInfo()
    {
        var assembly = typeof(SystemController).Assembly;
        return Ok(new SystemInfoResponse(
            Service: "ATT.Monitor.Api",
            Environment: environment.EnvironmentName,
            Version: assembly.GetName().Version?.ToString() ?? "0.0.0",
            Framework: RuntimeInformation.FrameworkDescription));
    }

    public sealed record SystemInfoResponse(
        string Service,
        string Environment,
        string Version,
        string Framework);
}
