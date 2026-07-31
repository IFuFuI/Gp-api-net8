using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>Paridad <c>api/Utility</c> → <c>api/Utility</c>.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UtilityController : ControllerBase
{
    [HttpGet("Test")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult TestConnection() => Ok("IDC_RESET");
}
