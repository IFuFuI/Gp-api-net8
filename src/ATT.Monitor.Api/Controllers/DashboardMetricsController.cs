using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DashboardMetricsController(IDashboardData dashboard) : ControllerBase
{
    [HttpPost("GetCards")]
    [ProducesResponseType(typeof(MCards), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCards()
    {
        var result = await dashboard.GetCardsAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }
}
