using ATT.Monitor.Api.Configuration;
using ATT.Monitor.Api.Models.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ATT.Monitor.Api.Controllers;

/// <summary>Metadatos públicos (sin JWT) consumidos por Monitor Blazor u operaciones.</summary>
[ApiController]
[Route("api/public")]
[AllowAnonymous]
public sealed class PublicMetadataController : ControllerBase
{
    /// <summary>Límite efectivo de cuerpo HTTP para multipart/ZIP (coherente con Kestrel/IIS).</summary>
    [HttpGet("upload-limits")]
    [ProducesResponseType(typeof(UploadLimitsResponseDto), StatusCodes.Status200OK)]
    public ActionResult<UploadLimitsResponseDto> GetUploadLimits(
        [FromServices] IOptionsMonitor<RequestBodyLimitsOptions> limits)
    {
        var o = limits.CurrentValue;
        var mb = Math.Clamp(o.MaxMegabytes, RequestBodyLimitsOptions.MinMegabytes, RequestBodyLimitsOptions.MaxMegabytesCap);
        var bytes = mb * 1024L * 1024L;
        return Ok(new UploadLimitsResponseDto(mb, bytes));
    }
}
