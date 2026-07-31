using System.Text.Json;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Infrastructure.Persistence;
using ATT.Monitor.Api.Models.Atm;
using ATT.Monitor.Api.Models.Crypto;
using ATT.Monitor.Api.Models.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>Paridad <c>api/DetalleAtm</c> (rutas bajo <c>api/DetalleAtm</c>).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DetalleAtmController(IAtmOperationsData atmOps, IDashboardData dashboard, ICrypto crypto) : ControllerBase
{
    private static readonly JsonSerializerOptions IdCajeroJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string MsgDatosDesencriptados = "Datos desencriptados inválidos.";
    private const string MsgDeserializacion = "No se pudo deserializar la solicitud.";

    [HttpPost("POST_ESTADO_ATM")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostEstadoAtm([FromBody] EstadoAtmRequest request)
    {
        var result = await atmOps.PostEstadoAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result))
            return NotFound($"No se pudo actualizar el estado del cajero {request.IdCajero}");
        return Ok(result);
    }

    [HttpPost("INSERT_COMANDO_ATM")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InsertComandoAtm([FromBody] InsertComandoAtmRequest? request)
    {
        if (request is null ||
            string.IsNullOrWhiteSpace(request.ID_CAJERO) ||
            string.IsNullOrWhiteSpace(request.COMANDO) ||
            string.IsNullOrWhiteSpace(request.ID_USUARIO))
            return BadRequest("Todos los campos son requeridos.");

        var result = await atmOps.InsertComandoAtmAsync(
                request.ID_CAJERO,
                request.ID_PAQUETE,
                request.COMANDO,
                request.ID_USUARIO,
                request.URL,
                HttpContext.RequestAborted)
            .ConfigureAwait(false);

        return Ok(result);
    }

    [HttpPost("GET_COMANDO_ATM")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComandoAtm([FromBody] EIdcajero request)
    {
        var result = await dashboard.ComandoAtmAsync(request.idcajero, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("GET_COMANDO_ATMCrip")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetComandoAtmCrip([FromBody] EncryptEnvelope? body)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.En))
            return BadRequest(MsgDatosDesencriptados);

        string decrypted;
        try
        {
            decrypted = crypto.Decrypt(body.En);
        }
        catch (Exception)
        {
            return BadRequest(MsgDatosDesencriptados);
        }

        if (string.IsNullOrWhiteSpace(decrypted))
            return BadRequest(MsgDatosDesencriptados);

        var request = JsonSerializer.Deserialize<EIdcajero>(decrypted, IdCajeroJson);
        if (request is null || string.IsNullOrWhiteSpace(request.idcajero))
            return BadRequest(MsgDeserializacion);

        var result = await dashboard.ComandoAtmAsync(request.idcajero, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("POST_STATUS_COMANDO")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateComandoAsync([FromBody] EIdSolComando request)
    {
        var result = await atmOps.UpdateComandoAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("POST_SISTEMA_INFO")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> InsertSistemaInfoAsync([FromBody] SistemaInfo request)
    {
        var result = await atmOps.InsertSistemaInfoAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("INSERT_STATUS_HW_DISPOSITIVOS")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> InsertStatusHwDispositivos([FromBody] CajeroAlarma request)
    {
        var result = await atmOps.InsertStatusHwDispositivosAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("INSERT_VERSION")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> InsertVersion([FromBody] EVersionAtm request)
    {
        var result = await atmOps.InsertVersionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }
}
