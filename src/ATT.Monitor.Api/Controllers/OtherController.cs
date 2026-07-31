using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>Paridad <c>api/Other</c> (v1 <c>JwtController</c>) → <c>api/Other</c>.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class OtherController(IWebHostEnvironment environment) : ControllerBase
{
    private const int AesKeySizeBytes = 32;
    private const int AesIvSizeBytes = 16;
    private const int MinJwtKeySize = 16;
    private const int MaxJwtKeySize = 64;
    private const int DefaultJwtKeySize = 32;

    [HttpGet("VerifyToken")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult VerifyToken()
    {
        var username = User.Identity?.Name;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new
        {
            success = true,
            message = "Token válido",
            user = username,
            role = role
        });
    }

    [HttpPost("GenerarClave")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<string> GenerarClave()
    {
        if (!environment.IsDevelopment())
            return NotFound();
        var clave = RandomNumberGenerator.GetBytes(AesKeySizeBytes);
        return Convert.ToBase64String(clave);
    }

    [HttpPost("GenerarIV")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<string> GenerarIV()
    {
        if (!environment.IsDevelopment())
            return NotFound();
        var iv = RandomNumberGenerator.GetBytes(AesIvSizeBytes);
        return Convert.ToBase64String(iv);
    }

    [HttpPost("GenerateKeyJWT")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<string> GenerateKeyJWT([FromQuery] int size = DefaultJwtKeySize)
    {
        if (!environment.IsDevelopment())
            return NotFound();
        if (size < MinJwtKeySize || size > MaxJwtKeySize)
            return BadRequest($"El tamaño debe estar entre {MinJwtKeySize} y {MaxJwtKeySize} bytes.");

        var key = RandomNumberGenerator.GetBytes(size);
        return Convert.ToBase64String(key);
    }
}
