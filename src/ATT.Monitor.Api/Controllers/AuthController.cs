using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>
/// Comprueba el pipeline JWT + validación en BD (requiere token válido).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AuthController : ControllerBase
{
    /// <summary>Reclamos del usuario autenticado (muestra de paridad con API v1).</summary>
    [HttpGet("principal")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetPrincipal()
    {
        return Ok(new
        {
            User.Identity?.Name,
            Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }
}
